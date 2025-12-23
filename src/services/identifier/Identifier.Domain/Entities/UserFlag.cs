namespace Identifier.Domain.Entities;

public class UserFlag
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid FeatureFlagId { get; set; }
    public string Variation { get; set; } = string.Empty;

    public User? User { get; set; }
    public FeatureFlag? FeatureFlag { get; set; }
}
