using Identifier.Domain.Entities;

namespace Identifier.Application.Authentication;

public sealed record AuthenticateUserCommand(string Email, string Password);

public sealed record AuthenticationResult(Guid UserId, string DisplayName, string Token, IReadOnlyCollection<string> Roles);

public interface IUserAuthenticator
{
    Task<AuthenticationResult?> AuthenticateAsync(AuthenticateUserCommand command, CancellationToken cancellationToken);
}

public sealed class AuthenticateUserHandler
{
    private readonly IUserAuthenticator _authenticator;

    public AuthenticateUserHandler(IUserAuthenticator authenticator)
    {
        _authenticator = authenticator;
    }

    public Task<AuthenticationResult?> HandleAsync(AuthenticateUserCommand command, CancellationToken cancellationToken) =>
        _authenticator.AuthenticateAsync(command, cancellationToken);
}
