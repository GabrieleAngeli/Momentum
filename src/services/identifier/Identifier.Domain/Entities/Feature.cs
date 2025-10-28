namespace Identifier.Domain.Entities;

public class Feature
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;

    public ICollection<ModuleFeature> Modules { get; set; } = new List<ModuleFeature>();
    public ICollection<Entitlement> Entitlements { get; set; } = new List<Entitlement>();
}
