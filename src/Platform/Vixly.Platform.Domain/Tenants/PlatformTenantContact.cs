using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantContact.cs
//
// PURPOSE:
// A named contact of a tenant: owner, billing or technical (D-T15).
//
// RESPONSIBILITIES:
// - Hold the role, name, email and optional phone
//
// IMPORTANT NOTES:
// - Owned by PlatformTenant; changed only through it
// - Notices and invoices go to these people, not to a single
//   company email as in V3
// =========================================================
public sealed class PlatformTenantContact : AuditableEntity
{
    private PlatformTenantContact()
    {
        Name = string.Empty;
        Email = null!;
    }

    private PlatformTenantContact(Guid platformTenantId, PlatformTenantContactDetails details, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformTenantId = platformTenantId;
        Role = details.Role;
        Name = details.Name;
        Email = details.Email;
        Phone = details.Phone;
    }

    public Guid PlatformTenantId { get; private set; }

    public PlatformTenantContactRole Role { get; private set; }

    public string Name { get; private set; }

    public EmailAddress Email { get; private set; }

    public PhoneNumber? Phone { get; private set; }

    internal static PlatformTenantContact Create(
        Guid platformTenantId,
        PlatformTenantContactDetails details,
        DateTimeOffset createdUtc)
    {
        return new PlatformTenantContact(platformTenantId, details, createdUtc);
    }

    internal bool Change(PlatformTenantContactDetails details, DateTimeOffset utcNow)
    {
        if (Name == details.Name && Email == details.Email && Phone == details.Phone)
        {
            return false;
        }

        Name = details.Name;
        Email = details.Email;
        Phone = details.Phone;
        MarkAsModified(utcNow);

        return true;
    }
}
