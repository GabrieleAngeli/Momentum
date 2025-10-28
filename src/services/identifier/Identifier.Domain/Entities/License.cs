namespace Identifier.Domain.Entities;

public class License
{
    public Guid Id { get; set; }
    public Guid OrganizationId { get; set; }
    public DateTimeOffset ValidFrom { get; set; }
    public DateTimeOffset ValidTo { get; set; }
    public string Tier { get; set; } = string.Empty;

    public Organization? Organization { get; set; }
    public ICollection<Entitlement> Entitlements { get; set; } = new List<Entitlement>();
    public ICollection<LicenseModule> Modules { get; set; } = new List<LicenseModule>();
}
