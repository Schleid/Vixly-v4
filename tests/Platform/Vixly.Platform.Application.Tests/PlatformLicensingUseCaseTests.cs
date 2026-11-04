using System.Security.Cryptography;
using System.Text;
using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Application.UseCases.Licensing.ApprovePlatformDevice;
using Vixly.Platform.Application.UseCases.Licensing.CompletePlatformDeviceLink;
using Vixly.Platform.Application.UseCases.Licensing.CreatePlatformDeviceChallenge;
using Vixly.Platform.Application.UseCases.Licensing.DecidePlatformDeviceRelease;
using Vixly.Platform.Application.UseCases.Licensing.EnrolPlatformDevice;
using Vixly.Platform.Application.UseCases.Licensing.GetPlatformPortalAccess;
using Vixly.Platform.Application.UseCases.Licensing.ListPlatformDevices;
using Vixly.Platform.Application.UseCases.Licensing.ReleasePlatformDevice;
using Vixly.Platform.Application.UseCases.Licensing.RequestPlatformFirstDeviceLink;
using Vixly.Platform.Application.UseCases.Licensing.StartPlatformOwnerRecovery;
using Vixly.Platform.Application.UseCases.Licensing.VerifyPlatformDevice;
using Vixly.Platform.Application.UseCases.TenantAccount.GetTenantDeviceSeats;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformLicensingUseCaseTests.cs
//
// PURPOSE:
// Proves Slice 7 licensing over fakes: seats, approval, both proof methods, releases, links, recovery, closing and portal access.
//
// RESPONSIBILITIES:
// - Seats never exceed the subscription; approvals need full access
// - Secret rotation and reuse; key-pair challenges with a real P-256 key
// - Releases within the allowance at once, over it to Vixly; lost devices stop at once
// - First-device link completes the registration and activates the tenant in one step
//
// IMPORTANT NOTES:
// - Tests assert codes and states, never text (L-1)
// =========================================================
public sealed class PlatformLicensingUseCaseTests
{
    private readonly LicensingTestKit _kit = new();

    private async Task<PlatformDeviceEnrolment> EnrolAsync(Guid tenantId, string? publicKey = null, string name = "Front desk PC")
    {
        var response = await new EnrolPlatformDeviceUseCase(_kit.Administration()).ExecuteAsync(
            LicensingTestKit.Caller(tenantId), new EnrolPlatformDeviceRequest { Name = name, Kind = PlatformDeviceKind.Pc, PublicKey = publicKey }, CancellationToken.None);
        Assert.True(response.Succeeded, response.Error?.Code);
        return response.Enrolment!;
    }

    private async Task<string?> ApproveAsync(Guid tenantId, Guid deviceId)
    {
        var response = await new ApprovePlatformDeviceUseCase(_kit.Administration()).ExecuteAsync(
            LicensingTestKit.Caller(tenantId), new ApprovePlatformDeviceRequest { DeviceId = deviceId }, CancellationToken.None);
        return response.Error?.Code;
    }

    private Task<PlatformDeviceCheck> VerifyAsync(Guid tenantId, Guid deviceId, string? secret = null, string? challenge = null, string? signature = null)
    {
        return new VerifyPlatformDeviceUseCase(_kit.Administration()).ExecuteAsync(
            LicensingTestKit.Caller(tenantId), new VerifyPlatformDeviceRequest { DeviceId = deviceId, Secret = secret, Challenge = challenge, Signature = signature }, CancellationToken.None)
            .ContinueWith(task => task.Result.Check!, TaskScheduler.Default);
    }

    [Fact]
    public async Task Approval_TakesAFreeSeat_NeverMoreThanBought_AndNeedsFullAccess()
    {
        (PlatformTenant tenant, _) = _kit.Tenant(licences: 2);
        PlatformDeviceEnrolment first = await EnrolAsync(tenant.Id);
        PlatformDeviceEnrolment second = await EnrolAsync(tenant.Id);
        PlatformDeviceEnrolment third = await EnrolAsync(tenant.Id);

        Assert.Equal(PlatformDeviceStatus.Pending, first.Status);
        Assert.NotNull(first.Secret);
        Assert.Null(await ApproveAsync(tenant.Id, first.DeviceId));
        Assert.Null(await ApproveAsync(tenant.Id, second.DeviceId));
        Assert.Equal("platform.licensing.no_free_seat", await ApproveAsync(tenant.Id, third.DeviceId));
        Assert.Equal(2, _kit.Store.Seats.Single().SeatsInUse);
        Assert.Contains(PlatformLicensingNoticeKind.DeviceAdded, _kit.Notices());
        Assert.Contains(_kit.Kit.Tenants.Tenants.Outbox, item => item.Type == PlatformMessageTypes.DeviceApproved);

        (PlatformTenant unpaid, _) = _kit.Tenant(paid: false);
        PlatformDeviceEnrolment waiting = await EnrolAsync(unpaid.Id);
        _kit.Kit.Tenants.Clock.Advance(TimeSpan.FromDays(30));
        Assert.Equal("platform.licensing.access_not_full", await ApproveAsync(unpaid.Id, waiting.DeviceId));
    }

    // Owner, 2026-10-21: the Licences tab counts the licences bought and in use.
    [Fact]
    public async Task Seats_CountWhatIsBoughtAndInUse()
    {
        (PlatformTenant tenant, _) = _kit.Tenant(licences: 3);
        PlatformDeviceEnrolment first = await EnrolAsync(tenant.Id);
        await EnrolAsync(tenant.Id);
        Assert.Null(await ApproveAsync(tenant.Id, first.DeviceId));

        GetTenantDeviceSeatsResponse seats = await new GetTenantDeviceSeatsUseCase(_kit.Administration()).ExecuteAsync(
            LicensingTestKit.Caller(tenant.Id), new GetTenantDeviceSeatsRequest(), CancellationToken.None);

        Assert.Equal(3, seats.Seats);
        Assert.Equal(1, seats.InUse);
        Assert.Null(seats.ScheduledSeats);
    }

    [Fact]
    public async Task AnotherTenantsDevice_IsNeverFound()
    {
        (PlatformTenant tenant, _) = _kit.Tenant();
        (PlatformTenant other, _) = _kit.Tenant();
        PlatformDeviceEnrolment device = await EnrolAsync(tenant.Id);

        Assert.Equal("platform.licensing.device_not_found", await ApproveAsync(other.Id, device.DeviceId));
        Assert.Equal("device_unknown", (await VerifyAsync(other.Id, device.DeviceId, device.Secret)).Code);
    }

    [Fact]
    public async Task Secret_PassesTheCheck_RotatesAfterADay_AndAReusedOldSecretRevokesTheDevice()
    {
        (PlatformTenant tenant, _) = _kit.Tenant();
        PlatformDeviceEnrolment enrolment = await EnrolAsync(tenant.Id);

        Assert.Equal("device_pending", (await VerifyAsync(tenant.Id, enrolment.DeviceId, enrolment.Secret)).Code);
        await ApproveAsync(tenant.Id, enrolment.DeviceId);

        PlatformDeviceCheck ok = await VerifyAsync(tenant.Id, enrolment.DeviceId, enrolment.Secret);
        Assert.True(ok.Allowed);
        Assert.Equal(PlatformSubscriptionAccess.Full, ok.Access);
        Assert.Null(ok.NewSecret);
        Assert.Equal("proof_invalid", (await VerifyAsync(tenant.Id, enrolment.DeviceId, "copied-wrong")).Code);

        _kit.Kit.Tenants.Clock.Advance(TimeSpan.FromHours(25));
        PlatformDeviceCheck rotated = await VerifyAsync(tenant.Id, enrolment.DeviceId, enrolment.Secret);
        Assert.True(rotated.Allowed);
        Assert.NotNull(rotated.NewSecret);
        Assert.True((await VerifyAsync(tenant.Id, enrolment.DeviceId, rotated.NewSecret)).Allowed);

        _kit.Kit.Tenants.Clock.Advance(TimeSpan.FromMinutes(6));
        Assert.Equal("device_revoked", (await VerifyAsync(tenant.Id, enrolment.DeviceId, enrolment.Secret)).Code);
        Assert.Equal(PlatformDeviceReason.CredentialReused, _kit.Store.Devices.Single().Reason);
        Assert.Contains(PlatformLicensingNoticeKind.CredentialReused, _kit.Notices());
        Assert.Equal(0, _kit.Store.Seats.Single().SeatsInUse);
    }

    [Fact]
    public async Task KeyPair_SignsAFreshChallenge_UsedOnce()
    {
        (PlatformTenant tenant, _) = _kit.Tenant();
        using ECDsa key = ECDsa.Create(ECCurve.NamedCurves.nistP256);
        PlatformDeviceEnrolment enrolment = await EnrolAsync(tenant.Id, Convert.ToBase64String(key.ExportSubjectPublicKeyInfo()));
        Assert.Null(enrolment.Secret);
        await ApproveAsync(tenant.Id, enrolment.DeviceId);

        string challenge = (await new CreatePlatformDeviceChallengeUseCase(_kit.Administration()).ExecuteAsync(
            LicensingTestKit.Caller(tenant.Id), new CreatePlatformDeviceChallengeRequest { DeviceId = enrolment.DeviceId }, CancellationToken.None)).Challenge;
        string signature = Convert.ToBase64String(key.SignData(Encoding.UTF8.GetBytes(challenge), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));

        Assert.Equal("proof_invalid", (await VerifyAsync(tenant.Id, enrolment.DeviceId, challenge: challenge, signature: Convert.ToBase64String(new byte[64]))).Code);
        Assert.Equal("proof_invalid", (await VerifyAsync(tenant.Id, enrolment.DeviceId, challenge: challenge, signature: signature)).Code);

        string fresh = (await new CreatePlatformDeviceChallengeUseCase(_kit.Administration()).ExecuteAsync(
            LicensingTestKit.Caller(tenant.Id), new CreatePlatformDeviceChallengeRequest { DeviceId = enrolment.DeviceId }, CancellationToken.None)).Challenge;
        string good = Convert.ToBase64String(key.SignData(Encoding.UTF8.GetBytes(fresh), HashAlgorithmName.SHA256, DSASignatureFormat.IeeeP1363FixedFieldConcatenation));
        Assert.True((await VerifyAsync(tenant.Id, enrolment.DeviceId, challenge: fresh, signature: good)).Allowed);
        Assert.Equal("proof_invalid", (await VerifyAsync(tenant.Id, enrolment.DeviceId, challenge: fresh, signature: good)).Code);

        var badKey = await new EnrolPlatformDeviceUseCase(_kit.Administration()).ExecuteAsync(
            LicensingTestKit.Caller(tenant.Id), new EnrolPlatformDeviceRequest { Name = "Scanner", Kind = PlatformDeviceKind.Scanner, PublicKey = "bm90LWEta2V5" }, CancellationToken.None);
        Assert.Equal("platform.licensing.public_key_invalid", badKey.Error!.Code);
    }

    [Fact]
    public async Task Releases_WithinTheAllowanceAreImmediate_OverItVixlyReviews_AndALostDeviceStopsAtOnce()
    {
        (PlatformTenant tenant, _) = _kit.Tenant(licences: 5);
        List<PlatformDeviceEnrolment> devices = new();
        for (int i = 0; i < 5; i++)
        {
            PlatformDeviceEnrolment enrolment = await EnrolAsync(tenant.Id, name: "PC " + i);
            await ApproveAsync(tenant.Id, enrolment.DeviceId);
            devices.Add(enrolment);
        }

        ReleasePlatformDeviceUseCase release = new(_kit.Administration());
        async Task<ReleasePlatformDeviceResponse> ReleaseAsync(int index, PlatformDeviceReason reason) => await release.ExecuteAsync(
            LicensingTestKit.Caller(tenant.Id), new ReleasePlatformDeviceRequest { DeviceId = devices[index].DeviceId, Reason = reason }, CancellationToken.None);

        Assert.Equal(PlatformDeviceStatus.Released, (await ReleaseAsync(0, PlatformDeviceReason.Replaced)).Status);
        Assert.Equal(PlatformDeviceStatus.Released, (await ReleaseAsync(1, PlatformDeviceReason.StaffLeft)).Status);

        ReleasePlatformDeviceResponse third = await ReleaseAsync(2, PlatformDeviceReason.Broken);
        Assert.Equal(PlatformDeviceStatus.ReleasePending, third.Status);
        Assert.True(third.AwaitsReview);

        ReleasePlatformDeviceResponse lost = await ReleaseAsync(3, PlatformDeviceReason.LostOrStolen);
        Assert.Equal(PlatformDeviceStatus.Revoked, lost.Status);
        Assert.True(lost.AwaitsReview);
        Assert.Equal("device_revoked", (await VerifyAsync(tenant.Id, devices[3].DeviceId, devices[3].Secret)).Code);
        Assert.Equal(3, _kit.Store.Seats.Single().SeatsInUse);

        var queue = await new ListPlatformDevicesUseCase(_kit.Administration()).ExecuteAsync(
            _kit.Kit.Staff(PlatformPermissions.LicensingView), new ListPlatformDevicesRequest { AwaitingReviewOnly = true }, CancellationToken.None);
        Assert.Equal(2, queue.Devices!.TotalCount);

        DecidePlatformDeviceReleaseUseCase decide = new(_kit.Administration());
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => decide.ExecuteAsync(_kit.Kit.Staff(PlatformPermissions.LicensingView),
            new DecidePlatformDeviceReleaseRequest { DeviceId = devices[2].DeviceId, Approve = true, Note = "ok" }, CancellationToken.None));

        var staff = _kit.Kit.Staff(PlatformPermissions.LicensingApproveRelease);
        Assert.True((await decide.ExecuteAsync(staff, new DecidePlatformDeviceReleaseRequest { DeviceId = devices[2].DeviceId, Approve = true, Note = "Confirmed with the owner" }, CancellationToken.None)).Succeeded);
        Assert.True((await decide.ExecuteAsync(staff, new DecidePlatformDeviceReleaseRequest { DeviceId = devices[3].DeviceId, Approve = true, Note = "Stolen laptop, police report" }, CancellationToken.None)).Succeeded);
        Assert.Equal(1, _kit.Store.Seats.Single().SeatsInUse);
        Assert.Contains(PlatformLicensingNoticeKind.ReleaseDecided, _kit.Notices());
    }

    [Fact]
    public async Task FirstDeviceLink_CompletesTheRegistration_AndActivatesTheTenant_InOneStep()
    {
        PlatformTenant tenant = _kit.Kit.Tenants.Tenant("orbit-freight");
        _kit.Kit.Subscribe(tenant.Id, _kit.Kit.Plan());
        PlatformTenantRegistration registration = PlatformTenantRegistration.Start(
            TenantTestKit.Company(), "Grace Hopper", Vixly.Shared.Kernel.ValueObjects.EmailAddress.Create("grace@orbit.example"), Vixly.Shared.Kernel.ValueObjects.PhoneNumber.Create("+1 514 555 0101"),
            Vixly.Shared.Kernel.Localization.LanguageCode.En, "America/Toronto", "orbit-freight", new string('a', 64), true, "2026-10", "2026-09", "203.0.113.7", _kit.Now);
        registration.MarkEmailVerified(tenant.Id, _kit.Now);
        registration.RecordPlanChosen(false, _kit.Now);
        _kit.Kit.Tenants.Tenants.Registrations.Add(registration);
        PlatformTenantAuthorizationContext caller = LicensingTestKit.Caller(tenant.Id);

        Assert.Equal("platform.licensing.registration_not_ready", (await new RequestPlatformFirstDeviceLinkUseCase(_kit.Administration()).ExecuteAsync(caller, new RequestPlatformFirstDeviceLinkRequest(), CancellationToken.None)).Error!.Code);
        registration.RecordPaid(_kit.Now);
        Assert.True((await new RequestPlatformFirstDeviceLinkUseCase(_kit.Administration()).ExecuteAsync(caller, new RequestPlatformFirstDeviceLinkRequest(), CancellationToken.None)).Succeeded);
        Assert.Equal(PlatformTenantRegistrationStatus.PendingFirstDeviceRegistration, registration.Status);

        await _kit.DeliverAsync();
        (string to, PlatformLicensingMail mail) = Assert.Single(_kit.Store.Mails);
        Assert.Equal("owner@northwind.example", to);
        string token = mail.Link!.Split('#')[1];

        CompletePlatformDeviceLinkUseCase complete = new(_kit.Administration());

        // Opened on another tenant's host: refused, and the link still works on its own host.
        Assert.Equal("platform.licensing.link_invalid", (await complete.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
            new CompletePlatformDeviceLinkRequest { Token = token, Name = "Owner laptop", Kind = PlatformDeviceKind.Pc, ExpectedTenantId = Guid.NewGuid() }, CancellationToken.None)).Error!.Code);

        var done = await complete.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
            new CompletePlatformDeviceLinkRequest { Token = token, Name = "Owner laptop", Kind = PlatformDeviceKind.Pc, ExpectedTenantId = tenant.Id }, CancellationToken.None);

        Assert.True(done.Succeeded, done.Error?.Code);
        Assert.Equal(PlatformDeviceStatus.Active, done.Enrolment!.Status);
        Assert.Equal(PlatformTenantRegistrationStatus.Completed, registration.Status);
        Assert.Equal(PlatformTenantStatus.Active, tenant.Status);
        Assert.Contains(_kit.Kit.Tenants.Tenants.Outbox, item => item.Type == PlatformMessageTypes.TenantStatusChanged);

        // The welcome email is the last one, with the staff sign-in and the public site.
        await _kit.DeliverAsync();
        PlatformLicensingMail welcome = _kit.Store.Mails.Single(item => item.Mail.Kind == PlatformLicensingNoticeKind.Welcome).Mail;
        Assert.Equal("https://orbit-freight.vixly.test/staff", welcome.StaffLink);
        Assert.Equal("https://orbit-freight.vixly.test/", welcome.SiteLink);

        Assert.Equal("platform.licensing.link_invalid", (await complete.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
            new CompletePlatformDeviceLinkRequest { Token = token, Name = "Second", Kind = PlatformDeviceKind.Pc }, CancellationToken.None)).Error!.Code);
    }

    [Fact]
    public async Task OwnerRecovery_ReplacesTheNamedDevice_WhenNoSeatIsFree()
    {
        (PlatformTenant tenant, _) = _kit.Tenant(licences: 2);
        PlatformDeviceEnrolment a = await EnrolAsync(tenant.Id);
        PlatformDeviceEnrolment b = await EnrolAsync(tenant.Id);
        await ApproveAsync(tenant.Id, a.DeviceId);
        await ApproveAsync(tenant.Id, b.DeviceId);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => new StartPlatformOwnerRecoveryUseCase(_kit.Administration()).ExecuteAsync(
            _kit.Kit.Staff(PlatformPermissions.LicensingView), new StartPlatformOwnerRecoveryRequest { PlatformTenantId = tenant.Id, Reason = "Lost laptop", IdentityCheck = "Called back" }, CancellationToken.None));
        Assert.True((await new StartPlatformOwnerRecoveryUseCase(_kit.Administration()).ExecuteAsync(
            _kit.Kit.Staff(PlatformPermissions.LicensingManage), new StartPlatformOwnerRecoveryRequest { PlatformTenantId = tenant.Id, Reason = "Lost laptop", IdentityCheck = "Called back the owner phone on file" }, CancellationToken.None)).Succeeded);
        await _kit.DeliverAsync();
        string token = _kit.Store.Mails.Single(item => item.Mail.Kind == PlatformLicensingNoticeKind.RecoveryLink).Mail.Link!.Split('#')[1];

        CompletePlatformDeviceLinkUseCase complete = new(_kit.Administration());
        Assert.Equal("platform.licensing.no_free_seat", (await complete.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
            new CompletePlatformDeviceLinkRequest { Token = token, Name = "New laptop", Kind = PlatformDeviceKind.Pc }, CancellationToken.None)).Error!.Code);
        var replaced = await complete.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance,
            new CompletePlatformDeviceLinkRequest { Token = token, Name = "New laptop", Kind = PlatformDeviceKind.Pc, ReplaceDeviceId = a.DeviceId }, CancellationToken.None);

        Assert.True(replaced.Succeeded, replaced.Error?.Code);
        Assert.Equal(PlatformDeviceReason.ReplacedByRecovery, _kit.Store.Devices.Single(item => item.Id == a.DeviceId).Reason);
        Assert.Equal(2, _kit.Store.Seats.Single().SeatsInUse);
        Assert.Contains(PlatformLicensingNoticeKind.DeviceReplaced, _kit.Notices());
    }

    [Fact]
    public async Task ClosedAccount_RevokesEveryDevice_AndTheClockExpiresOldRequests()
    {
        (PlatformTenant tenant, _) = _kit.Tenant();
        PlatformDeviceEnrolment active = await EnrolAsync(tenant.Id);
        await ApproveAsync(tenant.Id, active.DeviceId);
        await EnrolAsync(tenant.Id, name: "Waiting");

        _kit.Kit.Tenants.Clock.Advance(TimeSpan.FromDays(8));
        Assert.Equal(1, await new PlatformLicensingClock(_kit.Administration()).RunAsync(CancellationToken.None));
        Assert.Contains(_kit.Store.Devices, item => item.Status == PlatformDeviceStatus.Expired);

        _kit.Kit.Tenants.Outbox().Write(PlatformMessageTypes.TenantStatusChanged, tenant.Id,
            new PlatformTenantStatusChanged(tenant.Id, tenant.TenantCode, PlatformTenantStatus.Active, PlatformTenantStatus.Deactivated, null, _kit.Now));
        await new PlatformLicensingEndHandler(_kit.Administration()).HandleAsync(_kit.Kit.Tenants.Tenants.Outbox.Last(), CancellationToken.None);

        Assert.Equal(PlatformDeviceReason.TenantClosed, _kit.Store.Devices.Single(item => item.Id == active.DeviceId).Reason);
        Assert.Equal(0, _kit.Store.Seats.Single().SeatsInUse);
        Assert.Contains(PlatformLicensingNoticeKind.DevicesRevoked, _kit.Notices());
    }

    [Fact]
    public async Task PortalAccess_KnowsOnlyLiveTenants_AndReportsTheAccessLevel()
    {
        (PlatformTenant tenant, _) = _kit.Tenant();
        GetPlatformPortalAccessUseCase portal = new(_kit.Administration());

        var unknown = await portal.ExecuteAsync(PlatformInternalAuthorizationContext.Instance, new GetPlatformPortalAccessRequest { Slug = "nobody-here" }, CancellationToken.None);
        Assert.False(unknown.Access!.Found);

        var found = await portal.ExecuteAsync(PlatformInternalAuthorizationContext.Instance, new GetPlatformPortalAccessRequest { Slug = tenant.CurrentSlug.ToUpperInvariant() }, CancellationToken.None);
        Assert.True(found.Access!.Found);
        Assert.Equal(PlatformSubscriptionAccess.Full, found.Access.Access);
    }

    [Fact]
    public async Task PortalAccess_ShowsATenantBeingSetUp_OnlyWhenAsked()
    {
        PlatformTenant tenant = _kit.Kit.Tenants.Tenant("setup-" + Guid.NewGuid().ToString("N")[..6]);
        GetPlatformPortalAccessUseCase portal = new(_kit.Administration());

        var edge = await portal.ExecuteAsync(PlatformInternalAuthorizationContext.Instance, new GetPlatformPortalAccessRequest { Slug = tenant.CurrentSlug }, CancellationToken.None);
        Assert.False(edge.Access!.Found);

        var operations = await portal.ExecuteAsync(PlatformInternalAuthorizationContext.Instance, new GetPlatformPortalAccessRequest { Slug = tenant.CurrentSlug, IncludeSettingUp = true }, CancellationToken.None);
        Assert.True(operations.Access!.Found);
        Assert.Equal(PlatformTenantStatus.Draft, operations.Access.Status);
        Assert.Equal(PlatformSubscriptionAccess.None, operations.Access.Access);
    }
}
