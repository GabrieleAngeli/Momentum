namespace Identifier.Domain.Entities;

public class OrgFlag
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public Guid FeatureFlagId { get; set; }
    public string Variation { get; set; } = string.Empty;
    public string? RuleJson { get; set; }

    public Organization? Organization { get; set; }
    public FeatureFlag? FeatureFlag { get; set; }
}
