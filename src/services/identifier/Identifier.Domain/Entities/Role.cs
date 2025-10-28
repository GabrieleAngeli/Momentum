namespace Identifier.Domain.Entities;

public class Role
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<GroupRole> GroupRoles { get; set; } = new List<GroupRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
