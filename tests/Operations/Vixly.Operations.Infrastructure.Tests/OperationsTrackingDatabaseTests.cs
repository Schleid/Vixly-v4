using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Infrastructure.Persistence.Repositories;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsTrackingDatabaseTests.cs
//
// PURPOSE:
// Proves the tracking store of Slice 3A against SQL Server (ADR-0021).
//
// RESPONSIBILITIES:
// - Several events of one command project together before the save
// - The stored package state equals a replay of the stored events
// - One idempotency key per tenant; events stay in their tenant
// - The global SQL script keeps the table append-only for the app login
//
// IMPORTANT NOTES:
// - Runs against a throwaway SQL Server database
// =========================================================
public sealed class OperationsTrackingDatabaseTests : IClassFixture<OperationsDatabaseFixture>
{
    private readonly OperationsDatabaseFixture _database;

    public OperationsTrackingDatabaseTests(OperationsDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task OneCommandsEvents_ProjectTogether_AndTheStoredStateEqualsAReplay()
    {
        Guid tenant = Guid.NewGuid();
        Guid packageId;
        DateTimeOffset now = DateTimeOffset.UtcNow;

        await using (OperationsDbContext write = Context(tenant, out OperationsTenantScope scope))
        {
            OperationsTrackingRepository store = new(write);
            OperationsTracker tracker = new(store, scope, TimeProvider.System);
            OperationsParcel package = OperationsParcel.Create(tenant, null, now);
            store.Add(package);
            packageId = package.Id;

            await tracker.RecordAsync(Draft(packageId, OperationsTrackingCodes.ReceivedAtOrigin, "scan:a-0001", now, ("warehouse", "MIA")), CancellationToken.None);
            await tracker.RecordAsync(Draft(packageId, OperationsTrackingCodes.HoldOpened, "cmd:a-0002", now, ("reason", "Damaged")), CancellationToken.None);
            OperationsTrackingResult repeat = await tracker.RecordAsync(Draft(packageId, OperationsTrackingCodes.HoldOpened, "cmd:a-0002", now, ("reason", "Damaged")), CancellationToken.None);
            Assert.False(repeat.Created);
            await write.SaveChangesAsync();
        }

        await using OperationsDbContext read = _database.NewContext(tenant);
        OperationsParcel stored = await read.Parcels.SingleAsync(item => item.Id == packageId);
        List<OperationsTrackingEvent> events = await read.TrackingEvents.Where(item => item.SubjectId == packageId).ToListAsync();

        Assert.Equal(2, events.Count);
        Assert.Equal(OperationsMilestone.ReceivedAtOrigin, stored.Milestone);
        Assert.Equal(OperationsHoldReasons.Damaged, stored.HoldReasons);
        Assert.Equal(OperationsTrackingProjection.Replay(events), stored.State);

        await using OperationsDbContext other = _database.NewContext(Guid.NewGuid());
        Assert.Empty(await other.TrackingEvents.Where(item => item.SubjectId == packageId).ToListAsync());
    }

    [SqlServerFact]
    public async Task AKey_IsUniquePerTenant_ButMayRepeatInAnotherTenant()
    {
        Guid north = Guid.NewGuid();
        Guid south = Guid.NewGuid();

        await SaveEventAsync(north, "scan:same-key-01");
        await SaveEventAsync(south, "scan:same-key-01");

        await Assert.ThrowsAsync<DbUpdateException>(() => SaveEventAsync(north, "scan:same-key-01"));
    }

    [SqlServerFact]
    public async Task TwoWritersOnOneParcel_CannotOverwriteEachOther_AndARacedKeyIsRecovered()
    {
        Guid tenant = Guid.NewGuid();
        DateTimeOffset now = DateTimeOffset.UtcNow;
        Guid parcelId;

        await using (OperationsDbContext setup = _database.NewContext(tenant))
        {
            OperationsParcel parcel = OperationsParcel.Create(tenant, null, now);
            setup.Parcels.Add(parcel);
            await setup.SaveChangesAsync();
            parcelId = parcel.Id;
        }

        // Desk A opens a hold while handheld B puts the parcel away: B must not erase A's hold.
        await using OperationsDbContext deskA = Context(tenant, out OperationsTenantScope scopeA);
        await using OperationsDbContext handB = Context(tenant, out OperationsTenantScope scopeB);
        OperationsTracker trackerA = new(new OperationsTrackingRepository(deskA), scopeA, TimeProvider.System);
        OperationsTracker trackerB = new(new OperationsTrackingRepository(handB), scopeB, TimeProvider.System);
        await trackerA.RecordAsync(Draft(parcelId, OperationsTrackingCodes.HoldOpened, "cmd:race-a-01", now, ("reason", "Damaged")), CancellationToken.None);
        await trackerB.RecordAsync(Draft(parcelId, OperationsTrackingCodes.PutAway, "scan:race-b-01", now, ("location", "A-1")), CancellationToken.None);
        await deskA.SaveChangesAsync();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => handB.SaveChangesAsync());

        await using (OperationsDbContext read = _database.NewContext(tenant))
        {
            OperationsParcel stored = await read.Parcels.SingleAsync(item => item.Id == parcelId);
            Assert.Equal(OperationsHoldReasons.Damaged, stored.HoldReasons);
            Assert.Equal(OperationsTrackingProjection.Replay(await read.TrackingEvents.Where(item => item.SubjectId == parcelId).ToListAsync()), stored.State);
        }

        // The same hold sent again by a second writer that lost the race is recovered as the first event.
        await using OperationsDbContext retry = Context(tenant, out OperationsTenantScope scopeRetry);
        OperationsTracker trackerRetry = new(new OperationsTrackingRepository(retry), scopeRetry, TimeProvider.System);
        OperationsTrackingResult? recovered = await trackerRetry.RecoverRepeatAsync(Draft(parcelId, OperationsTrackingCodes.HoldOpened, "cmd:race-a-01", now, ("reason", "Damaged")), CancellationToken.None);
        Assert.False(recovered!.Created);
        Assert.Equal("cmd:race-a-01", recovered.Event!.IdempotencyKey);
    }

    [Fact]
    public void TheGlobalScript_KeepsTrackingEventsAppendOnly()
    {
        string root = AppContext.BaseDirectory;

        while (!File.Exists(Path.Combine(root, "Vixly.Operations.slnx")))
        {
            root = Path.GetDirectoryName(root) ?? throw new InvalidOperationException("Repository root not found.");
        }

        string script = File.ReadAllText(Path.Combine(root, "build", "scripts", "sql", "vixly-v4-database.sql"));

        Assert.Contains("DENY UPDATE, DELETE ON OBJECT::[ops].[TrackingEvents] TO [vixly_operations_app];", script, StringComparison.Ordinal);
        Assert.Contains("DENY UPDATE, DELETE ON OBJECT::[ops].[AuditEntries] TO [vixly_operations_app];", script, StringComparison.Ordinal);
    }

    // Written straight to the store, as a second writer racing the first would.
    private async Task SaveEventAsync(Guid tenant, string key)
    {
        await using OperationsDbContext write = _database.NewContext(tenant);
        OperationsParcel package = OperationsParcel.Create(tenant, null, DateTimeOffset.UtcNow);
        write.Parcels.Add(package);
        write.TrackingEvents.Add(OperationsTrackingEvent.Record(tenant, Draft(package.Id, OperationsTrackingCodes.PreAlerted, key, DateTimeOffset.UtcNow), DateTimeOffset.UtcNow));
        await write.SaveChangesAsync();
    }

    private OperationsDbContext Context(Guid tenant, out OperationsTenantScope scope)
    {
        scope = new OperationsTenantScope();
        scope.Enter(tenant);
        return _database.NewContext(scope);
    }

    private static OperationsTrackingDraft Draft(Guid packageId, string code, string key, DateTimeOffset at, params (string Name, string Value)[] arguments) =>
        new(OperationsTrackingSubject.Parcel, packageId, code, arguments.ToDictionary(item => item.Name, item => item.Value), at, key, OperationsTrackingActor.Staff, OperationsTrackingSource.Scan)
        {
            ActorUserId = StaffUser
        };

    private static readonly Guid StaffUser = Guid.NewGuid();
}
