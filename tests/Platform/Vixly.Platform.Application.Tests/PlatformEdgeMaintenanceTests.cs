using Vixly.Platform.Application.Maintenance;
using Vixly.Platform.Application.Messaging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Tests.Fakes;
using Vixly.Platform.Domain.Maintenance;
using Vixly.Platform.Domain.Messaging;

namespace Vixly.Platform.Application.Tests;

// =========================================================
// FILE: PlatformEdgeMaintenanceTests.cs
//
// PURPOSE:
// Proves Control tells the Cloudflare Worker the open maintenance windows (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - The whole list of windows not cancelled and not finished, with site codes, messages and the pass hash
// - The outbox handler and the hourly clock send it; without a Worker nothing is counted
//
// IMPORTANT NOTES:
// - The Worker is a recording fake over the in-memory window store
// =========================================================
public sealed class PlatformEdgeMaintenanceTests
{
    private readonly TenantTestKit _kit = new();

    private readonly FakeMaintenanceStore _windows = new();

    private readonly RecordingEdge _edge = new();

    [Fact]
    public async Task SendsTheOpenWindows_WithTheirSitesMessagesAndPassHash_NeverTheFinishedOrCancelled()
    {
        DateTimeOffset now = TenantTestKit.Now;
        PlatformMaintenanceWindow running = Window(now.AddMinutes(-1), now.AddHours(1), PlatformMaintenanceSites.StaffWorkspaces | PlatformMaintenanceSites.CustomerPortals, "abc");
        PlatformMaintenanceWindow scheduled = Window(now.AddDays(10), now.AddDays(10).AddHours(2), PlatformMaintenanceSites.PublicSite, null);
        PlatformMaintenanceWindow cancelled = Window(now.AddDays(2), now.AddDays(2).AddHours(1), PlatformMaintenanceSites.All, null);
        cancelled.Stop(null, now);
        _windows.Add(running);
        _windows.Add(scheduled);
        _windows.Add(cancelled);

        PlatformEdgeSendResult result = await new PlatformEdgeMaintenance(_windows, _edge, _kit.Clock).SendAsync(CancellationToken.None);

        Assert.Equal(PlatformEdgeSendResult.Sent, result);
        PlatformEdgeWindowList list = Assert.Single(_edge.Lists);
        Assert.Equal(new[] { running.Id, scheduled.Id }, list.Windows.Select(window => window.Id).ToArray());
        PlatformEdgeWindow first = list.Windows[0];
        Assert.Equal(new[] { "customers", "staff" }, first.Sites.OrderBy(site => site, StringComparer.Ordinal).ToArray());
        Assert.Equal("Scanners", first.Messages["en"]);
        Assert.Equal("Scanneurs", first.Messages["fr"]);
        Assert.False(first.Messages.ContainsKey("es"));
        Assert.Equal(running.PassHash, first.PassHash);
        Assert.Empty(list.Windows[1].Messages);
    }

    [Fact]
    public async Task TheHandlerAndTheClockSend_AndCountOnlyWhatTheWorkerTook()
    {
        PlatformEdgeMaintenance edge = new(_windows, _edge, _kit.Clock);
        PlatformEdgeMaintenanceHandler handler = new(edge);

        Assert.Equal(PlatformMessageTypes.EdgeMaintenanceChanged, handler.MessageType);
        await handler.HandleAsync(PlatformOutboxMessage.Create(PlatformMessageTypes.EdgeMaintenanceChanged, PlatformEdgeMaintenance.StreamId, "{}", TenantTestKit.Now), CancellationToken.None);
        Assert.Single(_edge.Lists);

        PlatformEdgeMaintenanceClock clock = new(edge);
        Assert.Equal(1, await clock.RunAsync(CancellationToken.None));
        _edge.Answer = PlatformEdgeSendResult.NotConfigured;
        Assert.Equal(0, await clock.RunAsync(CancellationToken.None));
        _edge.Answer = PlatformEdgeSendResult.Stale;
        Assert.Equal(0, await clock.RunAsync(CancellationToken.None));
    }

    [Fact]
    public async Task AWorkerThatRefuses_Throws_SoTheOutboxTriesAgain()
    {
        _edge.Fail = true;
        PlatformEdgeMaintenanceHandler handler = new(new PlatformEdgeMaintenance(_windows, _edge, _kit.Clock));

        await Assert.ThrowsAsync<HttpRequestException>(() => handler.HandleAsync(
            PlatformOutboxMessage.Create(PlatformMessageTypes.EdgeMaintenanceChanged, PlatformEdgeMaintenance.StreamId, "{}", TenantTestKit.Now), CancellationToken.None));
    }

    private PlatformMaintenanceWindow Window(DateTimeOffset starts, DateTimeOffset ends, PlatformMaintenanceSites sites, string? hash) =>
        PlatformMaintenanceWindow.Schedule(starts, ends, sites, 0, hash is null ? null : "Scanners", hash is null ? null : "Scanneurs", null,
            _kit.Crypto.HashToken(hash ?? Guid.NewGuid().ToString()), null, starts < TenantTestKit.Now ? starts : TenantTestKit.Now);

    private sealed class RecordingEdge : IPlatformEdgeClient
    {
        public List<PlatformEdgeWindowList> Lists { get; } = new();

        public PlatformEdgeSendResult Answer { get; set; } = PlatformEdgeSendResult.Sent;

        public bool Fail { get; set; }

        public Task<PlatformEdgeSendResult> SendMaintenanceAsync(PlatformEdgeWindowList list, CancellationToken cancellationToken)
        {
            if (Fail)
            {
                throw new HttpRequestException("refused");
            }

            Lists.Add(list);
            return Task.FromResult(Answer);
        }
    }
}
