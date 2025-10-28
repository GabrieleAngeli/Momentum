namespace Identifier.Domain.Entities;

public class ModuleFeature
{
    public Guid ModuleId { get; set; }
    public Guid FeatureId { get; set; }

    public Module? Module { get; set; }
    public Feature? Feature { get; set; }
}
