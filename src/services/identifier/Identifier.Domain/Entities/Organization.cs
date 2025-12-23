namespace Identifier.Domain.Entities;

public class Organization
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<Group> Groups { get; set; } = new List<Group>();
    public ICollection<License> Licenses { get; set; } = new List<License>();
    public ICollection<OrgFlag> Flags { get; set; } = new List<OrgFlag>();
}
