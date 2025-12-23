namespace Identifier.Domain.Entities;

public class Module
{
    public Guid Id { get; set; }
    public string Key { get; set; } = string.Empty;

    public ICollection<ModuleFeature> Features { get; set; } = new List<ModuleFeature>();
    public ICollection<LicenseModule> Licenses { get; set; } = new List<LicenseModule>();
}
