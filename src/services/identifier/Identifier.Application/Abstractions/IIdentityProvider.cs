namespace Identifier.Application.Abstractions;

public interface IIdentityProvider
{
    Guid? GetUserId();
    Guid? GetOrganizationId();
}
