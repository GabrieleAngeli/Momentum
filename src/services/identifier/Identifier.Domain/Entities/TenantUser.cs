using System;
using System.Collections.Generic;

namespace Identifier.Domain.Entities;

public sealed class TenantUser
{
    public Guid Id { get; }
    public string Email { get; }
    public string DisplayName { get; }
    public IReadOnlyCollection<string> Roles => _roles.AsReadOnly();
    public IReadOnlyCollection<string> FeatureFlags => _featureFlags.AsReadOnly();
    public DateTimeOffset LicenseValidUntil { get; private set; }

    private readonly List<string> _roles = new();
    private readonly List<string> _featureFlags = new();

    public TenantUser(Guid id, string email, string displayName, IEnumerable<string> roles, IEnumerable<string> featureFlags, DateTimeOffset licenseValidUntil)
    {
        Id = id;
        Email = email;
        DisplayName = displayName;
        LicenseValidUntil = licenseValidUntil;
        _roles.AddRange(roles);
        _featureFlags.AddRange(featureFlags);
    }

    public bool HasRole(string role) => _roles.Contains(role, StringComparer.OrdinalIgnoreCase);

    public bool IsLicenseValid(DateTimeOffset now) => now <= LicenseValidUntil;

    public void UpdateLicense(DateTimeOffset validUntil) => LicenseValidUntil = validUntil;
}
