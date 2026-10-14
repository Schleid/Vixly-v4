using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: TenantTestKit.cs
//
// PURPOSE:
// Shared wiring for tenant and registration use case tests.
//
// RESPONSIBILITIES:
// - Hold the fake stores, crypto and clock
// - Create signed-in staff with given permissions
// - Build the shared tenant services
//
// IMPORTANT NOTES:
// - Each test class owns one kit, so tests never share state
// =========================================================
internal sealed class TenantTestKit
{
    public static readonly DateTimeOffset Now = new(2026, 10, 14, 12, 0, 0, TimeSpan.Zero);

    public FakePlatformStore Store { get; } = new();

    public FakeTenantStore Tenants { get; } = new();

    public FakeCrypto Crypto { get; } = new();

    public FixedTimeProvider Clock { get; } = new(Now);

    public PlatformAuthorizationGate Gate()
    {
        return new PlatformAuthorizationGate(new PlatformAuthorizationService(Store, Clock));
    }

    public PlatformAuditTrail Audit()
    {
        return new PlatformAuditTrail(Store, Store, Clock);
    }

    public PlatformOutboxWriter Outbox()
    {
        return new PlatformOutboxWriter(Tenants, Clock);
    }

    public PlatformTenantAdministration Administration()
    {
        return new PlatformTenantAdministration(
            Gate(),
            new PlatformStepUpGuard(Store, Clock),
            Store,
            Tenants,
            Tenants,
            Audit(),
            Outbox(),
            Clock);
    }

    public PlatformPortalSlugs Slugs()
    {
        return new PlatformPortalSlugs(Tenants, Tenants);
    }

    public PlatformRegistrationLinks Links()
    {
        return new PlatformRegistrationLinks(Tenants, Tenants, Crypto);
    }

    public PlatformAuthorizationContext Staff(params string[] permissions)
    {
        return SignIn(PlatformUserAuthorityLevel.Standard, permissions);
    }

    public PlatformAuthorizationContext SignIn(PlatformUserAuthorityLevel level, params string[] permissions)
    {
        PlatformUser user = PlatformUser.Invite(
            "Staff",
            "Member" + Store.Users.Count,
            EmailAddress.Create($"staff{Store.Users.Count}@techcrafters.example"),
            LanguageCode.En,
            level,
            null,
            null,
            Now);
        user.AcceptInvitation("H:password", Now);
        user.BeginAuthenticatorEnrolment("encrypted-secret", Now);
        user.ConfirmAuthenticatorApp(1, Now);
        Store.Users.Add(user);

        if (permissions.Length > 0)
        {
            PlatformRole role = PlatformRole.CreateCustom("Role " + Store.Roles.Count, Now);
            role.ReplacePermissions(permissions, Now);
            Store.Roles.Add(role);
            user.AssignRole(role.Id, Guid.NewGuid(), null, Now);
        }

        PlatformUserSession session = PlatformUserSession.Start(user.Id, new string((char)('a' + Store.Sessions.Count), 64), null, null, Clock.GetUtcNow());
        Store.Sessions.Add(session);

        return new PlatformAuthorizationContext(user.Id, level, session.Id);
    }

    public PlatformTenant Tenant(string slug = "northwind", bool active = false)
    {
        PlatformTenantCreation creation = PlatformTenant.CreateByStaff(
            "TEN-" + (Tenants.Tenants.Count + 100),
            slug,
            Company(),
            PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, "Grace Hopper", EmailAddress.Create("owner@northwind.example"), null),
            LanguageCode.Fr,
            "America/Toronto",
            Guid.NewGuid(),
            Now);
        Tenants.Tenants.Add(creation.Tenant);
        Tenants.Slugs.Add(PlatformPortalSlug.ClaimForTenant(slug, creation.Tenant.Id, Now));

        if (active)
        {
            creation.Tenant.Activate(Guid.NewGuid(), Now);
        }

        return creation.Tenant;
    }

    public static PlatformCompanyDetails Company()
    {
        return Input().ToDetails();
    }

    public static PlatformCompanyInput Input()
    {
        return new PlatformCompanyInput
        {
            LegalName = "Northwind Freight Inc.",
            TradingName = "Northwind",
            Email = "office@northwind.example",
            Phone = "+1 514 555 0100",
            AddressLine1 = "1 Harbour Road",
            City = "Montreal",
            StateProvince = "QC",
            PostalCode = "H2X 1Y4",
            CountryCode = "CA"
        };
    }
}
