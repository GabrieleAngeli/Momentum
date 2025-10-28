namespace Identifier.Application.Abstractions;

public record AuthorizationDecision(bool Allowed, string Reason)
{
    public static AuthorizationDecision Success() => new(true, "allowed");
    public static AuthorizationDecision Denied(string reason) => new(false, reason);
}
