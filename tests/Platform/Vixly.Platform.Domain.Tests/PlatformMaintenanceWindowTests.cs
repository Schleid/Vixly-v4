using Vixly.Platform.Domain.Maintenance;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Tests;

// =========================================================
// FILE: PlatformMaintenanceWindowTests.cs
//
// PURPOSE:
// Proves the rules of a maintenance window (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Times, length, sites, notice and message are checked
// - The status follows the clock
// - Stop cancels before the start and ends early while running, once
//
// IMPORTANT NOTES:
// - Times are UTC
// =========================================================
public sealed class PlatformMaintenanceWindowTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Schedule_RefusesBadTimes_TooLong_NoSite_BadNotice_AndALongMessage()
    {
        Assert.Equal(PlatformMaintenanceErrors.TimesInvalid, Refused(() => Window(Now.AddHours(2), Now.AddHours(1))));
        Assert.Equal(PlatformMaintenanceErrors.TimesInvalid, Refused(() => Window(Now.AddHours(-1), Now.AddHours(1))));
        Assert.Equal(PlatformMaintenanceErrors.TooLong, Refused(() => Window(Now.AddHours(1), Now.AddDays(9))));
        Assert.Equal(PlatformMaintenanceErrors.SitesRequired, Refused(() => Window(Now.AddHours(1), Now.AddHours(2), PlatformMaintenanceSites.None)));
        Assert.Equal(PlatformMaintenanceErrors.NoticeInvalid, Refused(() => Window(Now.AddHours(1), Now.AddHours(2), notice: 200)));
        Assert.Equal(PlatformMaintenanceErrors.MessageTooLong, Refused(() => Window(Now.AddHours(1), Now.AddHours(2), message: new string('x', 501))));
    }

    [Fact]
    public void Status_FollowsTheClock()
    {
        PlatformMaintenanceWindow window = Window(Now.AddHours(72), Now.AddHours(74), notice: 48);

        Assert.Equal(PlatformMaintenanceStatus.Scheduled, window.StatusAt(Now));
        Assert.Equal(PlatformMaintenanceStatus.Announced, window.StatusAt(Now.AddHours(24)));
        Assert.Equal(PlatformMaintenanceStatus.Running, window.StatusAt(Now.AddHours(72)));
        Assert.Equal(PlatformMaintenanceStatus.Finished, window.StatusAt(Now.AddHours(74)));
        Assert.True(window.Closes(PlatformMaintenanceSites.StaffWorkspaces));
    }

    [Fact]
    public void Stop_CancelsBeforeTheStart_EndsEarlyWhileRunning_AndOnlyOnce()
    {
        PlatformMaintenanceWindow coming = Window(Now.AddHours(5), Now.AddHours(6));
        coming.Stop(Guid.NewGuid(), Now);
        Assert.Equal(PlatformMaintenanceStatus.Cancelled, coming.StatusAt(Now.AddHours(5)));
        Assert.Equal(PlatformMaintenanceErrors.AlreadyOver, Refused(() => coming.Stop(null, Now)));

        PlatformMaintenanceWindow running = Window(Now.AddMinutes(1), Now.AddHours(3));
        running.Stop(null, Now.AddHours(1));
        Assert.Equal(Now.AddHours(1), running.EndsUtc);
        Assert.Equal(PlatformMaintenanceStatus.Finished, running.StatusAt(Now.AddHours(1)));
        Assert.Equal(PlatformMaintenanceErrors.AlreadyOver, Refused(() => running.Stop(null, Now.AddHours(2))));
    }

    private static PlatformMaintenanceWindow Window(DateTimeOffset starts, DateTimeOffset ends, PlatformMaintenanceSites sites = PlatformMaintenanceSites.All, int notice = 48, string? message = "Upgrade")
    {
        return PlatformMaintenanceWindow.Schedule(starts, ends, sites, notice, message, null, null, "hash-" + Guid.NewGuid().ToString("N"), null, Now);
    }

    private static object Refused(Action action)
    {
        return Assert.Throws<DomainException>(action).Error;
    }
}
