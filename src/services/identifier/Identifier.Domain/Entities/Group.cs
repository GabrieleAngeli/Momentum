namespace Identifier.Domain.Entities;

public class Group
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Name { get; set; } = string.Empty;

    public Organization? Organization { get; set; }
    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    public ICollection<GroupRole> GroupRoles { get; set; } = new List<GroupRole>();
    public ICollection<GroupFlag> Flags { get; set; } = new List<GroupFlag>();
}
