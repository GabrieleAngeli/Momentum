namespace Identifier.Application;

public class IdentifierAuthorizationOptions
{
    public IDictionary<string, string> FeatureMap { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    public IDictionary<string, string> FlagMap { get; init; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
