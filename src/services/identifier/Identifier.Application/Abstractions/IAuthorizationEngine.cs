namespace Identifier.Application.Abstractions;

public interface IAuthorizationEngine
{
    Task<AuthorizationDecision> AuthorizeAsync(Guid userId, string resource, string action, CancellationToken cancellationToken = default);
}
