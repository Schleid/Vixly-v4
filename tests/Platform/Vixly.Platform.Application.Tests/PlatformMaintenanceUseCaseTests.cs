using Vixly.Platform.Application.Maintenance;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Application.UseCases.Maintenance.CheckPlatformMaintenancePass;
using Vixly.Platform.Application.UseCases.Maintenance.GetPlatformMaintenanceForSite;
using Vixly.Platform.Application.UseCases.Maintenance.ListPlatformMaintenanceWindows;
using Vixly.Platform.Application.UseCases.Maintenance.SchedulePlatformMaintenanceWindow;
using Vixly.Platform.Application.UseCases.Maintenance.StopPlatformMaintenanceWindow;
using Vixly.Platform.Domain.Maintenance;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformMaintenanceUseCaseTests.cs
//
// PURPOSE:
// Proves the maintenance window use cases (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Publishing needs platform.maintenance.manage, keeps only the pass hash and is audited
// - A site sees only announced and running windows that close it
// - The pass link opens a running window of its sites only
// - Stopping cancels, then refuses a second stop
//
// IMPORTANT NOTES:
// - Real gate, step-up guard and audit over the in-memory stores
// =========================================================
public sealed class PlatformMaintenanceUseCaseTests
{
    private readonly TenantTestKit _kit = new();

    private readonly FakeMaintenanceStore _windows = new();

    [Fact]
    public async Task Publish_NeedsTheRight_KeepsOnlyThePassHash_AndIsAudited()
    {
        PlatformAuthorizationContext reader = _kit.Staff(PlatformPermissions.MaintenanceView);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Schedule().ExecuteAsync(reader, Request(starts: TenantTestKit.Now.AddHours(1)), CancellationToken.None));

        SchedulePlatformMaintenanceWindowResponse response = await Schedule().ExecuteAsync(_kit.Staff(PlatformPermissions.MaintenanceManage), Request(starts: TenantTestKit.Now.AddHours(1)), CancellationToken.None);

        Assert.True(response.Succeeded, response.Error?.Code);
        PlatformMaintenanceWindow window = Assert.Single(_windows.Windows);
        Assert.NotEqual(response.PassToken, window.PassHash);
        Assert.Equal(_kit.Crypto.HashToken(response.PassToken), window.PassHash);
        Assert.Contains(_kit.Store.AuditEntries, entry => entry.ActionCode == "platform.maintenance.scheduled");
        // Owner, 2026-10-28: the Cloudflare Worker is told, in the same save.
        Assert.Contains(_kit.Tenants.Outbox, message => message.Type == Vixly.Platform.Application.Messaging.PlatformMessageTypes.EdgeMaintenanceChanged && message.StreamId == PlatformEdgeMaintenance.StreamId);

        ListPlatformMaintenanceWindowsResponse list = await new ListPlatformMaintenanceWindowsUseCase(Administration(), new PlatformAuthorizationService(_kit.Store, _kit.Clock))
            .ExecuteAsync(reader, new ListPlatformMaintenanceWindowsRequest(), CancellationToken.None);
        Assert.False(list.CanManage);
        Assert.Equal(PlatformMaintenanceStatus.Announced, Assert.Single(list.Open).Status);
    }

    [Fact]
    public async Task ASite_SeesOnlyAnnouncedAndRunningWindowsThatCloseIt_AndThePassOpensOnlyThose()
    {
        SchedulePlatformMaintenanceWindowResponse staffOnly = await Schedule().ExecuteAsync(_kit.Staff(PlatformPermissions.MaintenanceManage),
            Request(sites: new[] { PlatformMaintenanceSiteCodes.StaffWorkspaces }), CancellationToken.None);
        await Schedule().ExecuteAsync(_kit.Staff(PlatformPermissions.MaintenanceManage),
            Request(starts: TenantTestKit.Now.AddDays(30)), CancellationToken.None);

        GetPlatformMaintenanceForSiteResponse staff = await Site(PlatformMaintenanceSiteCodes.StaffWorkspaces);
        GetPlatformMaintenanceForSiteResponse portals = await Site(PlatformMaintenanceSiteCodes.CustomerPortals);
        GetPlatformMaintenanceForSiteResponse unknown = await Site("control");

        Assert.Single(staff.Windows);
        Assert.Empty(portals.Windows);
        Assert.Equal(PlatformCommonErrors.RequestInvalid, unknown.Error);

        CheckPlatformMaintenancePassUseCase pass = new(_windows, _kit.Crypto, _kit.Clock);
        Assert.True((await pass.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, new CheckPlatformMaintenancePassRequest { Token = staffOnly.PassToken, Site = "staff" }, CancellationToken.None)).Succeeded);
        Assert.False((await pass.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, new CheckPlatformMaintenancePassRequest { Token = staffOnly.PassToken, Site = "public" }, CancellationToken.None)).Succeeded);
        Assert.False((await pass.ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, new CheckPlatformMaintenancePassRequest { Token = "wrong", Site = "staff" }, CancellationToken.None)).Succeeded);
    }

    [Fact]
    public async Task Stop_Cancels_ThenRefusesASecondStop()
    {
        PlatformAuthorizationContext manager = _kit.Staff(PlatformPermissions.MaintenanceManage);
        SchedulePlatformMaintenanceWindowResponse scheduled = await Schedule().ExecuteAsync(manager, Request(starts: TenantTestKit.Now.AddHours(1)), CancellationToken.None);
        StopPlatformMaintenanceWindowUseCase stop = new(Administration());

        StopPlatformMaintenanceWindowResponse first = await stop.ExecuteAsync(manager, new StopPlatformMaintenanceWindowRequest { WindowId = scheduled.WindowId }, CancellationToken.None);
        StopPlatformMaintenanceWindowResponse second = await stop.ExecuteAsync(manager, new StopPlatformMaintenanceWindowRequest { WindowId = scheduled.WindowId }, CancellationToken.None);
        StopPlatformMaintenanceWindowResponse missing = await stop.ExecuteAsync(manager, new StopPlatformMaintenanceWindowRequest { WindowId = Guid.NewGuid() }, CancellationToken.None);

        Assert.True(first.Succeeded);
        Assert.Equal(2, _kit.Tenants.Outbox.Count(message => message.Type == Vixly.Platform.Application.Messaging.PlatformMessageTypes.EdgeMaintenanceChanged));
        Assert.Equal(PlatformMaintenanceErrors.AlreadyOver, second.Error);
        Assert.Equal(PlatformMaintenanceErrors.NotFound, missing.Error);
        Assert.Empty((await Site(PlatformMaintenanceSiteCodes.PublicSite)).Windows);
    }

    private SchedulePlatformMaintenanceWindowUseCase Schedule() => new(Administration());

    private Task<GetPlatformMaintenanceForSiteResponse> Site(string site) =>
        new GetPlatformMaintenanceForSiteUseCase(_windows, _kit.Clock).ExecuteAsync(PlatformAnonymousAuthorizationContext.Instance, new GetPlatformMaintenanceForSiteRequest { Site = site }, CancellationToken.None);

    private PlatformMaintenanceAdministration Administration() =>
        new(_kit.Gate(), new PlatformStepUpGuard(_kit.Store, _kit.Clock), _windows, _kit.Crypto, _kit.Store, _kit.Audit(), _kit.Clock, _kit.Outbox());

    // Running now by default: started a minute ago, for two hours.
    private static SchedulePlatformMaintenanceWindowRequest Request(DateTimeOffset? starts = null, IReadOnlyList<string>? sites = null) => new()
    {
        StartsUtc = starts ?? TenantTestKit.Now.AddMinutes(-1),
        EndsUtc = (starts ?? TenantTestKit.Now).AddHours(2),
        Sites = sites ?? PlatformMaintenanceSiteCodes.All.Select(item => item.Code).ToArray(),
        NoticeHours = 48,
        MessageEn = "Database upgrade"
    };
}
