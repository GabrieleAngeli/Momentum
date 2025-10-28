namespace Identifier.Domain.Entities;

public class FeatureFlag
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;
    public string DefaultVariation { get; set; } = string.Empty;

    public ICollection<OrgFlag> OrgFlags { get; set; } = new List<OrgFlag>();
    public ICollection<GroupFlag> GroupFlags { get; set; } = new List<GroupFlag>();
    public ICollection<UserFlag> UserFlags { get; set; } = new List<UserFlag>();
}
