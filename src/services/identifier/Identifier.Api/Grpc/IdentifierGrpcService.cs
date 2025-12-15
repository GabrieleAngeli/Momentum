using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Identifier.Api.Seed;
using Identifier.Application.Abstractions;
using Identifier.Infrastructure.Persistence;
using Identifier.Infrastructure.Seeding;
using Microsoft.EntityFrameworkCore;

namespace Identifier.Api.Grpc;

public sealed class IdentifierGrpcService : Identifier.Rpc.V1.IdentifierService.IdentifierServiceBase
{
    private readonly IAuthorizationEngine _authorizationEngine;
    private readonly IFeatureFlagProvider _featureFlagProvider;
    private readonly ILicenseService _licenseService;
    private readonly IdentifierDbContext _dbContext;
    private readonly IdentifierSeeder _seeder;
    private readonly IConfiguration _configuration;

    public IdentifierGrpcService(
        IAuthorizationEngine authorizationEngine,
        IFeatureFlagProvider featureFlagProvider,
        ILicenseService licenseService,
        IdentifierDbContext dbContext,
        IdentifierSeeder seeder,
        IConfiguration configuration)
    {
        _authorizationEngine = authorizationEngine;
        _featureFlagProvider = featureFlagProvider;
        _licenseService = licenseService;
        _dbContext = dbContext;
        _seeder = seeder;
        _configuration = configuration;
    }

    public override async Task<Identifier.Rpc.V1.AuthorizeResponse> Authorize(Identifier.Rpc.V1.AuthorizeRequest request, ServerCallContext context)
    {
        if (!Guid.TryParse(request.UserId, out var userId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "user_id must be a valid GUID"));
        }

        if (string.IsNullOrWhiteSpace(request.Resource) || string.IsNullOrWhiteSpace(request.Action))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "resource and action are required"));
        }

        var decision = await _authorizationEngine.AuthorizeAsync(userId, request.Resource, request.Action, context.CancellationToken);

        return new Identifier.Rpc.V1.AuthorizeResponse
        {
            Allowed = decision.Allowed,
            Reason = decision.Reason
        };
    }

    public override async Task<Identifier.Rpc.V1.EvaluateFlagResponse> EvaluateFlag(Identifier.Rpc.V1.EvaluateFlagRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.FlagKey))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "flag_key is required"));
        }

        Guid? organizationId = null;
        if (!string.IsNullOrWhiteSpace(request.OrganizationId))
        {
            if (!Guid.TryParse(request.OrganizationId, out var parsedOrgId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "organization_id must be a valid GUID"));
            }

            organizationId = parsedOrgId;
        }

        Guid? userId = null;
        if (!string.IsNullOrWhiteSpace(request.UserId))
        {
            if (!Guid.TryParse(request.UserId, out var parsedUserId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "user_id must be a valid GUID"));
            }

            userId = parsedUserId;
        }

        var groupIds = new List<Guid>(request.GroupIds.Count);
        foreach (var groupId in request.GroupIds)
        {
            if (!Guid.TryParse(groupId, out var parsedGroupId))
            {
                throw new RpcException(new Status(StatusCode.InvalidArgument, "group_ids must contain valid GUIDs"));
            }

            groupIds.Add(parsedGroupId);
        }

        var flag = await _dbContext.FeatureFlags
            .AsNoTracking()
            .Where(f => f.Key == request.FlagKey)
            .Select(f => new { f.Id })
            .FirstOrDefaultAsync(context.CancellationToken);

        if (flag is null)
        {
            throw new RpcException(new Status(StatusCode.NotFound, $"Feature flag '{request.FlagKey}' not found"));
        }

        var variation = await _featureFlagProvider.EvaluateAsync(flag.Id, organizationId, userId, groupIds.ToArray(), context.CancellationToken);
        var enabled = await _featureFlagProvider.IsEnabledAsync(request.FlagKey, organizationId, userId, groupIds.ToArray(), context.CancellationToken);

        return new Identifier.Rpc.V1.EvaluateFlagResponse
        {
            FlagKey = request.FlagKey,
            Variation = variation,
            Enabled = enabled
        };
    }

    public override async Task<Identifier.Rpc.V1.CheckLicenseResponse> CheckLicense(Identifier.Rpc.V1.CheckLicenseRequest request, ServerCallContext context)
    {
        if (string.IsNullOrWhiteSpace(request.FeatureKey))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "feature_key is required"));
        }

        if (!Guid.TryParse(request.OrganizationId, out var organizationId))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "organization_id must be a valid GUID"));
        }

        var evaluation = await _licenseService.EvaluateAsync(organizationId, request.FeatureKey, context.CancellationToken);

        var response = new Identifier.Rpc.V1.CheckLicenseResponse
        {
            OrganizationId = request.OrganizationId,
            FeatureKey = request.FeatureKey,
            HasLicense = evaluation.HasLicense,
            FeatureIncluded = evaluation.FeatureIncluded,
            WithinQuota = evaluation.WithinQuota,
            Reason = evaluation.Reason ?? string.Empty
        };

        if (evaluation.RemainingQuota.HasValue)
        {
            response.RemainingQuota = evaluation.RemainingQuota.Value; // oppure evaluation.RemainingQuota
        }
        else
        {
            response.RemainingQuota = null;
        }

        return response;
    }

    public override async Task<Identifier.Rpc.V1.SeedResponse> Seed(Identifier.Rpc.V1.SeedRequest request, ServerCallContext context)
    {
        if (!request.Force && !SeedExecution.IsSeedEnabled(_configuration))
        {
            throw new RpcException(new Status(StatusCode.PermissionDenied, "Seeding is disabled"));
        }

        await _seeder.SeedAsync(context.CancellationToken);
        return new Identifier.Rpc.V1.SeedResponse { Seeded = true };
    }
}
