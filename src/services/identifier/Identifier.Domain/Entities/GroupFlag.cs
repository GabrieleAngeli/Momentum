namespace Identifier.Domain.Entities;

public class GroupFlag
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid FeatureFlagId { get; set; }
    public string Variation { get; set; } = string.Empty;

    public Group? Group { get; set; }
    public FeatureFlag? FeatureFlag { get; set; }
}
