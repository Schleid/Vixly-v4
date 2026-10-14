using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Domain.Tenants;

// =========================================================
// FILE: PlatformTenantContactDetails.cs
//
// PURPOSE:
// The input for one named tenant contact (D-T15).
//
// RESPONSIBILITIES:
// - Validate the role, name, email and optional phone
//
// IMPORTANT NOTES:
// - Immutable input; PlatformTenant turns it into a contact row
// =========================================================
public sealed class PlatformTenantContactDetails
{
    private PlatformTenantContactDetails(
        PlatformTenantContactRole role,
        string name,
        EmailAddress email,
        PhoneNumber? phone)
    {
        Role = role;
        Name = name;
        Email = email;
        Phone = phone;
    }

    public PlatformTenantContactRole Role { get; }

    public string Name { get; }

    public EmailAddress Email { get; }

    public PhoneNumber? Phone { get; }

    public static PlatformTenantContactDetails Create(
        PlatformTenantContactRole role,
        string? name,
        EmailAddress email,
        PhoneNumber? phone)
    {
        ArgumentNullException.ThrowIfNull(email);

        if (!Enum.IsDefined(role))
        {
            throw new DomainException(PlatformTenantErrors.ContactRoleInvalid);
        }

        return new PlatformTenantContactDetails(
            role,
            PlatformTenantText.Required(
                name,
                PlatformTenantLimits.ContactNameMaxLength,
                PlatformTenantErrors.ContactNameRequired,
                PlatformTenantErrors.ContactNameTooLong),
            email,
            phone);
    }
}
