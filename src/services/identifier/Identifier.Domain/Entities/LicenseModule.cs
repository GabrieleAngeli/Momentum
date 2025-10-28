namespace Identifier.Domain.Entities;

public class LicenseModule
{
    public Guid LicenseId { get; set; }
    public Guid ModuleId { get; set; }

    public License? License { get; set; }
    public Module? Module { get; set; }
}
