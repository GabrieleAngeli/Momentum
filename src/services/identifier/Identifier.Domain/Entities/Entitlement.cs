namespace Identifier.Domain.Entities;

public class Entitlement
{
    public Guid Id { get; set; }
    public Guid LicenseId { get; set; }
    public Guid FeatureId { get; set; }
    public int? Quota { get; set; }
    public string? ConstraintsJson { get; set; }

    public License License { get; set; } = null!;
    public Feature Feature { get; set; } = null!;
}
