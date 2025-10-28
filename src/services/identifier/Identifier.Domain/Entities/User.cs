namespace Identifier.Domain.Entities;

public class User
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public string Email { get; set; } = string.Empty;
    public bool Active { get; set; }

    public Organization? Organization { get; set; }
    public ICollection<UserGroup> UserGroups { get; set; } = new List<UserGroup>();
    public ICollection<UserFlag> Flags { get; set; } = new List<UserFlag>();
}
