using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Platform.Application.UseCases.Tenants;
using Vixly.Platform.Application.UseCases.Tenants.ActivatePlatformTenant;
using Vixly.Platform.Application.UseCases.Tenants.AddPlatformTenantNote;
using Vixly.Platform.Application.UseCases.Tenants.AssignPlatformTenantAccountManager;
using Vixly.Platform.Application.UseCases.Tenants.BanPlatformTenant;
using Vixly.Platform.Application.UseCases.Tenants.CorrectPlatformTenantSlug;
using Vixly.Platform.Application.UseCases.Tenants.SuspendPlatformTenant;
using Vixly.Platform.Application.UseCases.Tenants.UpdatePlatformTenantContacts;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Domain.Messaging;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformTenantAdministrationTests.cs
//
// PURPOSE:
// Proves the staff tenant use cases: lifecycle, slug
// correction, contacts, account manager and notes.
//
// RESPONSIBILITIES:
// - Permissions, step-up and SuperUser rules are enforced
// - Each change writes its history, audit and outbox entries
// - Slug claims follow the tenant (D-T10, D-T11)
//
// IMPORTANT NOTES:
// - Tests assert error codes, never text (L-1)
// - Staff no longer create tenants: a tenant comes only from its
//   own registration (Slice 2 sign-off correction, D-T8)
// =========================================================
public sealed class PlatformTenantAdministrationTests
{
    private readonly TenantTestKit _kit = new();

    [Fact]
    public async Task Suspend_NeedsStepUp_ThenRecordsHistoryAuditAndOutbox_WithoutTheReason()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);
        PlatformAuthorizationContext context = _kit.Staff(PlatformPermissions.TenantsSuspend);

        _kit.Clock.Advance(TimeSpan.FromMinutes(6));
        Assert.Equal(PlatformCommonErrors.StepUpRequired, (await SuspendAsync(context, tenant)).Error);
        Assert.Equal(PlatformTenantStatus.Active, tenant.Status);

        PlatformAuthorizationContext fresh = _kit.Staff(PlatformPermissions.TenantsSuspend);
        SuspendPlatformTenantResponse response = await SuspendAsync(fresh, tenant);

        Assert.True(response.Succeeded);
        Assert.Equal(PlatformTenantStatus.Suspended, tenant.Status);
        Assert.Equal(PlatformTenantEventCodes.Suspended, Assert.Single(_kit.Tenants.Events).EventCode);
        Assert.Equal("platform.tenants.suspended", Assert.Single(_kit.Store.AuditEntries).ActionCode);
        PlatformOutboxMessage message = Assert.Single(_kit.Tenants.Outbox);
        Assert.Equal(PlatformMessageTypes.TenantStatusChanged, message.Type);
        Assert.Equal(tenant.Id, message.StreamId);
        Assert.DoesNotContain("Unpaid compliance", message.Payload, StringComparison.Ordinal);
        Assert.Contains("\"toStatus\":3", message.Payload, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Ban_NeedsSuperUserAuthority()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);
        PlatformAuthorizationContext standard = _kit.Staff(PlatformPermissions.TenantsBan);
        PlatformAuthorizationContext superUser = _kit.SignIn(PlatformUserAuthorityLevel.SuperUser);

        BanPlatformTenantResponse refused = await BanAsync(standard, tenant);
        BanPlatformTenantResponse banned = await BanAsync(superUser, tenant);

        Assert.Equal(PlatformTenantAdminErrors.BanNeedsSuperUser, refused.Error);
        Assert.True(banned.Succeeded);
        Assert.Equal(PlatformTenantStatus.Banned, tenant.Status);
    }

    [Fact]
    public async Task Abilities_OfferBanOnlyToASuperUser()
    {
        PlatformAuthorizationContext standard = _kit.Staff(PlatformPermissions.TenantsBan, PlatformPermissions.TenantsEdit);

        PlatformTenantAbilities abilities = await _kit.Administration().ReadAbilitiesAsync(standard, CancellationToken.None);

        Assert.True(abilities.CanEdit);
        Assert.False(abilities.CanBan);
        Assert.True((await _kit.Administration().ReadAbilitiesAsync(_kit.SignIn(PlatformUserAuthorityLevel.SuperUser), CancellationToken.None)).CanBan);
    }

    [Fact]
    public async Task Activate_IsRefusedFromSuspended_WithTheMoveNamed()
    {
        PlatformTenant tenant = _kit.Tenant(active: true);
        tenant.Suspend(PlatformSuspensionCategory.Security, "x", Guid.NewGuid(), TenantTestKit.Now);
        PlatformAuthorizationContext context = _kit.Staff(PlatformPermissions.TenantsActivate);

        ActivatePlatformTenantResponse response = await new ActivatePlatformTenantUseCase(_kit.Administration(), _kit.Store)
            .ExecuteAsync(context, new ActivatePlatformTenantRequest { PlatformTenantId = tenant.Id }, CancellationToken.None);

        Assert.Equal(PlatformTenantErrors.MoveNotAllowed(PlatformTenantStatus.Suspended, PlatformTenantStatus.Active), response.Error);
        Assert.Empty(_kit.Tenants.Outbox);
    }

    [Fact]
    public async Task CorrectSlug_MovesTheClaim_OnlyWhileNeverActive()
    {
        PlatformTenant draft = _kit.Tenant("northwnd");
        PlatformTenant live = _kit.Tenant("live-co", active: true);
        PlatformAuthorizationContext context = _kit.Staff(PlatformPermissions.TenantsEdit);

        CorrectPlatformTenantSlugResponse corrected = await CorrectAsync(context, draft, "northwind");
        CorrectPlatformTenantSlugResponse refused = await CorrectAsync(context, live, "live-company");

        Assert.True(corrected.Succeeded);
        Assert.Equal("northwind", draft.CurrentSlug);
        Assert.DoesNotContain(_kit.Tenants.Slugs, claim => claim.Slug == "northwnd");
        Assert.Contains(_kit.Tenants.Slugs, claim => claim.Slug == "northwind" && claim.PlatformTenantId == draft.Id);
        Assert.Equal(PlatformMessageTypes.TenantSlugCorrected, Assert.Single(_kit.Tenants.Outbox).Type);
        Assert.Equal(PlatformTenantErrors.SlugFixed, refused.Error);
        Assert.Equal("live-co", live.CurrentSlug);
    }

    [Fact]
    public async Task CorrectSlug_RefusesAnotherTenantsSlug_AndTheSameSlug()
    {
        PlatformTenant draft = _kit.Tenant("northwnd");
        _kit.Tenant("northwind");
        PlatformAuthorizationContext context = _kit.Staff(PlatformPermissions.TenantsEdit);

        Assert.Equal(PlatformTenantErrors.SlugTaken, (await CorrectAsync(context, draft, "northwind")).Error);
        Assert.Equal(PlatformTenantErrors.SlugUnchanged, (await CorrectAsync(context, draft, "NORTHWND")).Error);
    }

    [Fact]
    public async Task AccountManager_MustBeAnActiveUser()
    {
        PlatformTenant tenant = _kit.Tenant();
        PlatformAuthorizationContext context = _kit.Staff(PlatformPermissions.TenantsEdit);
        PlatformUser manager = _kit.Store.Users[0];
        AssignPlatformTenantAccountManagerUseCase useCase = new(_kit.Administration(), _kit.Store, _kit.Store);

        AssignPlatformTenantAccountManagerResponse unknown = await useCase.ExecuteAsync(
            context,
            new AssignPlatformTenantAccountManagerRequest { PlatformTenantId = tenant.Id, PlatformUserId = Guid.NewGuid() },
            CancellationToken.None);
        AssignPlatformTenantAccountManagerResponse assigned = await useCase.ExecuteAsync(
            context,
            new AssignPlatformTenantAccountManagerRequest { PlatformTenantId = tenant.Id, PlatformUserId = manager.Id },
            CancellationToken.None);

        Assert.Equal(PlatformTenantErrors.AccountManagerInvalid, unknown.Error);
        Assert.True(assigned.Succeeded);
        Assert.Equal(manager.Id, tenant.AccountManagerUserId);
    }

    [Fact]
    public async Task Contacts_AreReplaced_AndAudited()
    {
        PlatformTenant tenant = _kit.Tenant();
        PlatformAuthorizationContext context = _kit.Staff(PlatformPermissions.TenantsEdit);

        UpdatePlatformTenantContactsResponse response = await new UpdatePlatformTenantContactsUseCase(_kit.Administration(), _kit.Store).ExecuteAsync(
            context,
            new UpdatePlatformTenantContactsRequest
            {
                PlatformTenantId = tenant.Id,
                Contacts = new[]
                {
                    new PlatformContactInput { Role = PlatformTenantContactRole.Owner, Name = "Grace Hopper", Email = "owner@northwind.example" },
                    new PlatformContactInput { Role = PlatformTenantContactRole.Billing, Name = "Ada Byron", Email = "billing@northwind.example", Phone = " " }
                }
            },
            CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal("billing@northwind.example", tenant.ContactFor(PlatformTenantContactRole.Billing).Email.Value);
        Assert.Equal("platform.tenants.contacts_updated", Assert.Single(_kit.Store.AuditEntries).ActionCode);
    }

    [Fact]
    public async Task Note_NeedsItsOwnPermission_AndItsTextStaysOutOfTheAudit()
    {
        PlatformTenant tenant = _kit.Tenant();
        AddPlatformTenantNoteUseCase useCase = new(_kit.Gate(), _kit.Tenants, _kit.Audit(), _kit.Store, _kit.Clock);
        AddPlatformTenantNoteRequest request = new() { PlatformTenantId = tenant.Id, Body = "Prefers calls after 3pm" };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            useCase.ExecuteAsync(_kit.Staff(PlatformPermissions.TenantsEdit), request, CancellationToken.None));

        AddPlatformTenantNoteResponse response = await useCase.ExecuteAsync(_kit.Staff(PlatformPermissions.TenantsAddNotes), request, CancellationToken.None);

        Assert.True(response.Succeeded);
        Assert.Equal("Prefers calls after 3pm", Assert.Single(_kit.Tenants.Notes).Body);
        Assert.DoesNotContain("3pm", Assert.Single(_kit.Store.AuditEntries).ArgumentsJson ?? string.Empty, StringComparison.Ordinal);
    }

    private Task<SuspendPlatformTenantResponse> SuspendAsync(PlatformAuthorizationContext context, PlatformTenant tenant)
    {
        return new SuspendPlatformTenantUseCase(_kit.Administration(), _kit.Store).ExecuteAsync(
            context,
            new SuspendPlatformTenantRequest
            {
                PlatformTenantId = tenant.Id,
                Category = PlatformSuspensionCategory.Compliance,
                Reason = "Unpaid compliance review"
            },
            CancellationToken.None);
    }

    private Task<BanPlatformTenantResponse> BanAsync(PlatformAuthorizationContext context, PlatformTenant tenant)
    {
        return new BanPlatformTenantUseCase(_kit.Administration(), _kit.Store).ExecuteAsync(
            context,
            new BanPlatformTenantRequest { PlatformTenantId = tenant.Id, Reason = "Fraud confirmed" },
            CancellationToken.None);
    }

    private Task<CorrectPlatformTenantSlugResponse> CorrectAsync(PlatformAuthorizationContext context, PlatformTenant tenant, string slug)
    {
        return new CorrectPlatformTenantSlugUseCase(_kit.Administration(), _kit.Slugs(), _kit.Tenants, _kit.Tenants, _kit.Outbox(), _kit.Store)
            .ExecuteAsync(
                context,
                new CorrectPlatformTenantSlugRequest { PlatformTenantId = tenant.Id, Slug = slug, Reason = "Typo at sign-up" },
                CancellationToken.None);
    }
}
