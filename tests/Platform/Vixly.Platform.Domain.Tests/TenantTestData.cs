using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: TenantTestData.cs
//
// PURPOSE:
// Builders and assertions for tenant tests.
//
// RESPONSIBILITIES:
// - Create staff-made tenants, registrations and details at a fixed
//   time
// - Assert domain error codes
//
// IMPORTANT NOTES:
// - Times are fixed UTC values; tests never read the clock
// =========================================================
internal static class TenantTestData
{
    public static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    public static readonly Guid Actor = Guid.Parse("0199e3a0-0000-7000-8000-000000000001");

    public static readonly string Hash = new('a', 64);

    public static PlatformPostalAddress Address()
    {
        return PlatformPostalAddress.Create("1 Harbour Road", null, "Montreal", "QC", "H2X 1Y4", "ca");
    }

    public static PlatformCompanyDetails Company(string legalName = "Northwind Freight Inc.")
    {
        return PlatformCompanyDetails.Create(
            legalName,
            "Northwind",
            EmailAddress.Create("office@northwind.example"),
            PhoneNumber.Create("+1 514 555 0100"),
            Address());
    }

    public static PlatformTenantContactDetails Contact(
        PlatformTenantContactRole role = PlatformTenantContactRole.Owner,
        string email = "owner@northwind.example")
    {
        return PlatformTenantContactDetails.Create(role, "Grace Hopper", EmailAddress.Create(email), null);
    }

    public static PlatformTenant Draft(string slug = "northwind")
    {
        return PlatformTenant.CreateByStaff(
            "TEN-7K3M9Q",
            slug,
            Company(),
            Contact(),
            LanguageCode.Fr,
            "America/Toronto",
            Actor,
            Now).Tenant;
    }

    public static PlatformTenant InStatus(PlatformTenantStatus status)
    {
        PlatformTenant tenant = Draft();

        switch (status)
        {
            case PlatformTenantStatus.Draft:
                break;
            case PlatformTenantStatus.Active:
                tenant.Activate(Actor, Now);
                break;
            case PlatformTenantStatus.Suspended:
                tenant.Activate(Actor, Now);
                tenant.Suspend(PlatformSuspensionCategory.Compliance, "Missing documents", Actor, Now);
                break;
            case PlatformTenantStatus.Deactivated:
                tenant.Activate(Actor, Now);
                tenant.Deactivate("Business closed", Actor, Now);
                break;
            case PlatformTenantStatus.Banned:
                tenant.Ban("Fraud confirmed", Actor, Now);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(status));
        }

        return tenant;
    }

    public static PlatformTenantRegistration Registration(bool termsAccepted = true)
    {
        return PlatformTenantRegistration.Start(
            Company(),
            "Grace Hopper",
            EmailAddress.Create("grace@northwind.example"),
            PhoneNumber.Create("+1 514 555 0101"),
            LanguageCode.Es,
            "America/Toronto",
            "Northwind",
            Hash,
            termsAccepted,
            "2026-10",
            "2026-09",
            "203.0.113.7",
            Now);
    }

    public static void AssertError(Error expected, Action action)
    {
        DomainException exception = Assert.Throws<DomainException>(action);
        Assert.Equal(expected, exception.Error);
    }
}
