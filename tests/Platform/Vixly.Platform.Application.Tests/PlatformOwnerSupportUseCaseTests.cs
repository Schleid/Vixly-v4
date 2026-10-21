using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing.ApprovePlatformDevice;
using Vixly.Platform.Application.UseCases.Licensing.CompletePlatformDeviceLink;
using Vixly.Platform.Application.UseCases.Licensing.CompletePlatformSecondFactorReset;
using Vixly.Platform.Application.UseCases.Licensing.DecidePlatformSecondFactorReset;
using Vixly.Platform.Application.UseCases.Licensing.EnrolPlatformDevice;
using Vixly.Platform.Application.UseCases.Licensing.RequestPlatformSecondFactorReset;
using Vixly.Platform.Application.UseCases.Licensing.StartPlatformOwnerRecovery;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformOwnerSupportUseCaseTests.cs
//
// PURPOSE:
// Proves owner support through Vixly staff only (D-O13): recovery with an identity check, and a second-factor reset with a second approver.
//
// RESPONSIBILITIES:
// - Recovery records the reason and identity check and replaces the device support named
// - A reset waits for a second staff member, then emails the Owner a link and tells Owner and Billing
// - The link works once, on its own tenant only
//
// IMPORTANT NOTES:
// - In-memory fakes
// =========================================================
public sealed class PlatformOwnerSupportUseCaseTests
{
    private readonly LicensingTestKit _kit = new();

    [Fact]
    public async Task Recovery_RecordsTheIdentityCheck_AndReplacesTheDeviceSupportNamed()
    {
        (PlatformTenant tenant, _) = _kit.Tenant(licences: 2);
        Billing(tenant);
        Guid lost = await ActiveDeviceAsync(tenant.Id);
        await ActiveDeviceAsync(tenant.Id);

        StartPlatformOwnerRecoveryUseCase start = new(_kit.Administration());
        Assert.Equal("platform.licensing.identity_check_required", (await start.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.LicensingManage),
            new StartPlatformOwnerRecoveryRequest { PlatformTenantId = tenant.Id, Reason = "Laptop stolen" }, CancellationToken.None)).Error?.Code);
        Assert.Equal("platform.licensing.device_not_found", (await start.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.LicensingManage),
            new StartPlatformOwnerRecoveryRequest { PlatformTenantId = tenant.Id, Reason = "Laptop stolen", IdentityCheck = "Called back", ReplaceDeviceId = Guid.NewGuid() }, CancellationToken.None)).Error?.Code);
        var started = await start.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.LicensingManage),
            new StartPlatformOwnerRecoveryRequest { PlatformTenantId = tenant.Id, Reason = "Laptop stolen", IdentityCheck = "Called back the owner phone on file", ReplaceDeviceId = lost }, CancellationToken.None);
        Assert.True(started.Succeeded, started.Error?.Code);

        PlatformOwnerSupportRequest support = Assert.Single(_kit.Store.SupportRequests);
        Assert.Equal(PlatformOwnerSupportStatus.Approved, support.Status);
        Assert.Equal("Called back the owner phone on file", support.IdentityCheck);

        await _kit.DeliverAsync();
        Assert.Equal(2, _kit.Store.Mails.Count(item => item.Mail.Kind == PlatformLicensingNoticeKind.OwnerSupportStarted));
        Assert.Contains(_kit.Store.Mails, item => item.Mail.Kind == PlatformLicensingNoticeKind.OwnerSupportStarted && item.To == "billing@support.example");
        string token = _kit.Store.Mails.Single(item => item.Mail.Kind == PlatformLicensingNoticeKind.RecoveryLink).Mail.Link!.Split('#')[1];

        // Every seat is taken: the device support named gives its seat without the owner choosing.
        var done = await new CompletePlatformDeviceLinkUseCase(_kit.Administration()).ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
            new CompletePlatformDeviceLinkRequest { Token = token, Name = "New laptop", Kind = PlatformDeviceKind.Pc, ExpectedTenantId = tenant.Id }, CancellationToken.None);

        Assert.True(done.Succeeded, done.Error?.Code);
        Assert.Equal(PlatformDeviceReason.ReplacedByRecovery, _kit.Store.Devices.Single(item => item.Id == lost).Reason);
        Assert.Equal(PlatformOwnerSupportStatus.Completed, support.Status);
        await _kit.DeliverAsync();
        Assert.Equal(2, _kit.Store.Mails.Count(item => item.Mail.Kind == PlatformLicensingNoticeKind.DeviceReplaced));
    }

    [Fact]
    public async Task SecondFactorReset_NeedsASecondStaffMember_ThenTheLinkWorksOnce_OnItsTenant()
    {
        (PlatformTenant tenant, _) = _kit.Tenant();
        Billing(tenant);
        PlatformAuthorizationContext alice = _kit.Kit.Staff(PlatformPermissions.LicensingManage);
        PlatformAuthorizationContext bob = _kit.Kit.Staff(PlatformPermissions.LicensingManage);

        var asked = await new RequestPlatformSecondFactorResetUseCase(_kit.Administration()).ExecuteAsync(alice,
            new RequestPlatformSecondFactorResetRequest { PlatformTenantId = tenant.Id, Reason = "Lost phone and codes", IdentityCheck = "Called back the owner phone on file" }, CancellationToken.None);
        Assert.True(asked.Succeeded, asked.Error?.Code);
        await _kit.DeliverAsync();
        Assert.Empty(_kit.Store.Mails);

        DecidePlatformSecondFactorResetUseCase decide = new(_kit.Administration());
        Assert.Equal("platform.licensing.approver_must_differ", (await decide.ExecuteAsync(alice,
            new DecidePlatformSecondFactorResetRequest { RequestId = asked.RequestId, Approve = true }, CancellationToken.None)).Error?.Code);
        Assert.True((await decide.ExecuteAsync(bob, new DecidePlatformSecondFactorResetRequest { RequestId = asked.RequestId, Approve = true, Note = "Checked" }, CancellationToken.None)).Succeeded);

        await _kit.DeliverAsync();
        (string to, PlatformLicensingMail mail) = Assert.Single(_kit.Store.Mails, item => item.Mail.Kind == PlatformLicensingNoticeKind.SecondFactorResetLink);
        Assert.Equal("owner@northwind.example", to);
        Assert.StartsWith("https://" + tenant.CurrentSlug + ".vixly.test/account/second-factor-reset#", mail.Link, StringComparison.Ordinal);
        Assert.Equal(2, _kit.Store.Mails.Count(item => item.Mail.Kind == PlatformLicensingNoticeKind.OwnerSupportStarted && item.Mail.SupportKind == PlatformOwnerSupportKind.SecondFactorReset));
        string token = mail.Link!.Split('#')[1];

        CompletePlatformSecondFactorResetUseCase complete = new(_kit.Administration());
        Assert.Equal("platform.licensing.link_invalid", (await complete.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
            new CompletePlatformSecondFactorResetRequest { Token = token, ExpectedTenantId = Guid.NewGuid() }, CancellationToken.None)).Error?.Code);
        var used = await complete.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
            new CompletePlatformSecondFactorResetRequest { Token = token, ExpectedTenantId = tenant.Id }, CancellationToken.None);
        Assert.True(used.Succeeded, used.Error?.Code);
        Assert.Equal(tenant.Id, used.PlatformTenantId);
        Assert.Equal(PlatformOwnerSupportStatus.Completed, _kit.Store.SupportRequests.Single().Status);
        Assert.Equal("platform.licensing.link_invalid", (await complete.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
            new CompletePlatformSecondFactorResetRequest { Token = token, ExpectedTenantId = tenant.Id }, CancellationToken.None)).Error?.Code);

        // A reset link never enrols a device.
        Assert.Equal("platform.licensing.link_invalid", (await new CompletePlatformDeviceLinkUseCase(_kit.Administration()).ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
            new CompletePlatformDeviceLinkRequest { Token = token, Name = "X", Kind = PlatformDeviceKind.Pc }, CancellationToken.None)).Error?.Code);

        await _kit.DeliverAsync();
        Assert.Equal(2, _kit.Store.Mails.Count(item => item.Mail.Kind == PlatformLicensingNoticeKind.SecondFactorResetDone));
    }

    [Fact]
    public async Task RefusedReset_SendsNothing_AndListsNoMore()
    {
        (PlatformTenant tenant, _) = _kit.Tenant();
        PlatformAuthorizationContext alice = _kit.Kit.Staff(PlatformPermissions.LicensingManage);
        PlatformAuthorizationContext bob = _kit.Kit.Staff(PlatformPermissions.LicensingManage, PlatformPermissions.LicensingView);

        var asked = await new RequestPlatformSecondFactorResetUseCase(_kit.Administration()).ExecuteAsync(alice,
            new RequestPlatformSecondFactorResetRequest { PlatformTenantId = tenant.Id, Reason = "Lost phone", IdentityCheck = "Could not call back" }, CancellationToken.None);
        Assert.True((await new DecidePlatformSecondFactorResetUseCase(_kit.Administration()).ExecuteAsync(bob,
            new DecidePlatformSecondFactorResetRequest { RequestId = asked.RequestId, Approve = false, Note = "Caller could not be confirmed" }, CancellationToken.None)).Succeeded);

        await _kit.DeliverAsync();
        Assert.Empty(_kit.Store.Mails);
        Assert.Equal(PlatformOwnerSupportStatus.Refused, _kit.Store.SupportRequests.Single().Status);
        Assert.Empty(await _kit.Store.ListOpenSupportRequestsAsync(tenant.Id, CancellationToken.None));
    }

    private static void Billing(PlatformTenant tenant)
    {
        PlatformTenantContact owner = tenant.ContactFor(PlatformTenantContactRole.Owner);
        Assert.True(tenant.ReplaceContacts(
            new[]
            {
                PlatformTenantContactDetails.Create(PlatformTenantContactRole.Owner, owner.Name, owner.Email, null),
                PlatformTenantContactDetails.Create(PlatformTenantContactRole.Billing, "Accounts", EmailAddress.Create("billing@support.example"), null)
            },
            DateTimeOffset.UtcNow));
    }

    private async Task<Guid> ActiveDeviceAsync(Guid tenantId)
    {
        var enrolled = await new EnrolPlatformDeviceUseCase(_kit.Administration()).ExecuteAsync(
            LicensingTestKit.Caller(tenantId), new EnrolPlatformDeviceRequest { Name = "Owner laptop", Kind = PlatformDeviceKind.Pc }, CancellationToken.None);
        Assert.True(enrolled.Succeeded, enrolled.Error?.Code);
        var approved = await new ApprovePlatformDeviceUseCase(_kit.Administration()).ExecuteAsync(
            LicensingTestKit.Caller(tenantId), new ApprovePlatformDeviceRequest { DeviceId = enrolled.Enrolment!.DeviceId }, CancellationToken.None);
        Assert.True(approved.Succeeded, approved.Error?.Code);
        return enrolled.Enrolment.DeviceId;
    }
}
