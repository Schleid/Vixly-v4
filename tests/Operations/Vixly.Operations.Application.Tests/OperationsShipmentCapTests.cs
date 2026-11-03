using Vixly.Operations.Application.Plans;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsShipmentCapTests.cs
//
// PURPOSE:
// Checks the plan's monthly shipment cap at the counter and in the portal (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Up to 150 percent of the cap new shipments are allowed (above the cap they are charged by Platform)
// - The first time above 150 percent is the one-time grace month; later months stop at 150 percent
// - No cap, or Platform not reachable: nothing is refused
// - The warning level and the company's calendar month
//
// IMPORTANT NOTES:
// - The counter is a fake; months follow the company's time zone
// =========================================================
public sealed class OperationsShipmentCapTests
{
    private static readonly Guid Tenant = Guid.Parse("01930000-0000-7000-8000-0000000000c1");

    private static readonly DateTimeOffset October = new(2026, 10, 20, 15, 0, 0, TimeSpan.Zero);

    private readonly FakePlanFeatures _features = new();

    private readonly FakeCounter _counter = new();

    private readonly FakeIdentityStore _store = new() { Settings = OperationsTenantSettings.CreateDefault(Tenant, October.AddMonths(-3)) };

    [Fact]
    public async Task UpTo150Percent_Allowed_ThenTheGraceMonthOnce_ThenStopped()
    {
        _features.Tenants[Tenant] = OperationsPlanFeatures.All with { ShipmentCap = 100, ExtraShipmentPrice = 0.5m, CurrencyCode = "USD" };
        OperationsShipmentCap cap = Cap();

        _counter.Count = 149;
        Assert.Null(await cap.RefuseNewAsync(Tenant, October, CancellationToken.None));

        // The 151st shipment of October: the one-time grace month, recorded.
        _counter.Count = 150;
        Assert.Null(await cap.RefuseNewAsync(Tenant, October, CancellationToken.None));
        Assert.Equal("2026-10", _store.Settings!.ShipmentGraceMonth);
        Assert.Null(await cap.RefuseNewAsync(Tenant, October.AddDays(5), CancellationToken.None));

        // November: stopped at 150 percent, with the numbers.
        Shared.Kernel.Errors.Error? refused = await cap.RefuseNewAsync(Tenant, October.AddMonths(1), CancellationToken.None);
        Assert.Equal("ops.shipments.plan_limit_reached", refused?.Code);
        Assert.Equal("2026-10", _store.Settings.ShipmentGraceMonth);

        OperationsShipmentCapStatus status = await cap.StatusAsync(Tenant, October.AddMonths(1), CancellationToken.None);
        Assert.True(status.Blocked);
        Assert.Equal(150, status.Level);
        Assert.Equal(50, status.Extra);
        Assert.Equal(150, status.BlockAt);
    }

    [Fact]
    public async Task NoCap_OrPlatformUnknown_NothingIsRefused()
    {
        _counter.Count = 10_000;
        _features.Tenants[Tenant] = OperationsPlanFeatures.All;
        Assert.Null(await Cap().RefuseNewAsync(Tenant, October, CancellationToken.None));

        _features.Tenants[Tenant] = OperationsPlanFeatures.Unknown with { ShipmentCap = 10 };
        Assert.Null(await Cap().RefuseNewAsync(Tenant, October, CancellationToken.None));
        Assert.Null(_store.Settings!.ShipmentGraceMonth);
    }

    [Fact]
    public void Levels_AndTheCompanysMonth()
    {
        Assert.Equal(0, OperationsShipmentCap.LevelOf(79, 100));
        Assert.Equal(80, OperationsShipmentCap.LevelOf(80, 100));
        Assert.Equal(100, OperationsShipmentCap.LevelOf(124, 100));
        Assert.Equal(125, OperationsShipmentCap.LevelOf(125, 100));
        Assert.Equal(150, OperationsShipmentCap.LevelOf(151, 100));
        Assert.Equal(0, OperationsShipmentCap.LevelOf(500, 0));
        Assert.Equal(225, OperationsShipmentCap.BlockAt(150));

        // 1 November 02:00 UTC is still 31 October in New York.
        (string month, DateTimeOffset start, DateTimeOffset end) = OperationsShipmentCap.MonthOf(new DateTimeOffset(2026, 11, 1, 2, 0, 0, TimeSpan.Zero), "America/New_York");
        Assert.Equal("2026-10", month);
        Assert.Equal(new DateTimeOffset(2026, 10, 1, 4, 0, 0, TimeSpan.Zero), start);
        Assert.Equal(new DateTimeOffset(2026, 11, 1, 4, 0, 0, TimeSpan.Zero), end);
        Assert.Equal("2026-09", OperationsShipmentCap.MonthOf(October, "America/New_York", monthsBack: 1).Month);
    }

    private OperationsShipmentCap Cap() => new(_features, _counter, _store, _store);

    private sealed class FakeCounter : IOperationsShipmentCounter
    {
        public int Count { get; set; }

        public Task<int> CountCreatedAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken) => Task.FromResult(Count);

        public Task<IReadOnlyList<Guid>> ListTenantsCreatingSinceAsync(DateTimeOffset sinceUtc, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Guid>>(new[] { Tenant });
    }
}
