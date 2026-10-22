using Vixly.Operations.Application.Audit;
using Vixly.Operations.Domain.Audit;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Infrastructure.Persistence.Repositories;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsSettingsDatabaseTests.cs
//
// PURPOSE:
// Proves the Slice 1E settings, numbering and audit reading against SQL Server.
//
// RESPONSIBILITIES:
// - Settings round-trip and stay in their tenant
// - Numbers are unique under concurrency, per tenant, kind and year
// - The audit log filters by person, area and date, newest first, paged
//
// IMPORTANT NOTES:
// - Runs against a throwaway SQL Server database
// =========================================================
public sealed class OperationsSettingsDatabaseTests : IClassFixture<OperationsDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly OperationsDatabaseFixture _database;

    public OperationsSettingsDatabaseTests(OperationsDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task Settings_RoundTrip_AndStayInTheirTenant()
    {
        Guid tenant = Guid.NewGuid();
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(tenant, Now);
        settings.ChangeCurrency("HTG", false, Now);
        settings.ChangeWork(OperationsWeightUnit.Pound, OperationsDimensionUnit.Inch, true, true, OperationsDeliveryModes.Pickup | OperationsDeliveryModes.HomeDelivery, Now);
        settings.ChangePrefixes("ENV", "REC", "FAC", "PAG", Now);
        settings.RequireSecondFactor(true, Now);

        await using (OperationsDbContext write = _database.NewContext(tenant))
        {
            new OperationsSettingsRepository(write).Add(settings);
            await write.SaveChangesAsync();
        }

        await using OperationsDbContext read = _database.NewContext(tenant);
        OperationsTenantSettings found = (await new OperationsSettingsRepository(read).FindAsync(CancellationToken.None))!;
        Assert.Equal("HTG", found.CurrencyCode);
        Assert.Equal(OperationsWeightUnit.Pound, found.WeightUnit);
        Assert.Equal(OperationsDeliveryModes.Pickup | OperationsDeliveryModes.HomeDelivery, found.DeliveryModes);
        Assert.True(found.RequireSecondFactorForAllStaff);
        Assert.Equal("FAC", found.Prefix(OperationsNumberKind.Invoice));

        await using OperationsDbContext other = _database.NewContext(Guid.NewGuid());
        Assert.Null(await new OperationsSettingsRepository(other).FindAsync(CancellationToken.None));

        OperationsShippingServiceSetup ground = OperationsShippingServiceSetup.Create(tenant, "ground", Now);
        ground.Change(true, "Camion", 1, 2, 5000m, true, Now);
        await using (OperationsDbContext write = _database.NewContext(tenant))
        {
            new OperationsShippingServiceRepository(write).Add(ground);
            await write.SaveChangesAsync();
        }

        await using OperationsDbContext again = _database.NewContext(tenant);
        OperationsShippingServiceSetup found2 = Assert.Single(await new OperationsShippingServiceRepository(again).ListAsync(CancellationToken.None));
        Assert.Equal(5000m, found2.VolumetricDivisor);
        Assert.Equal("Camion", found2.Label);
        Assert.Empty(await new OperationsShippingServiceRepository(other).ListAsync(CancellationToken.None));
    }

    [SqlServerFact]
    public async Task Numbers_AreUnique_UnderConcurrency()
    {
        Guid tenant = Guid.NewGuid();

        long[] numbers = await Task.WhenAll(Enumerable.Range(0, 25).Select(_ => Task.Run(() => NextAsync(tenant, OperationsNumberKind.Shipment, 2026))));

        Assert.Equal(Enumerable.Range(1, 25).Select(number => (long)number), numbers.Order());
    }

    [SqlServerFact]
    public async Task Numbers_RestartPerKindYearAndTenant()
    {
        Guid tenant = Guid.NewGuid();

        Assert.Equal(1, await NextAsync(tenant, OperationsNumberKind.Shipment, 2026));
        Assert.Equal(2, await NextAsync(tenant, OperationsNumberKind.Shipment, 2026));
        Assert.Equal(1, await NextAsync(tenant, OperationsNumberKind.Invoice, 2026));
        Assert.Equal(1, await NextAsync(tenant, OperationsNumberKind.Shipment, 2027));
        Assert.Equal(1, await NextAsync(Guid.NewGuid(), OperationsNumberKind.Shipment, 2026));
        Assert.Equal("SH26000042", Vixly.Operations.Application.Settings.OperationsNumbering.Format("SH", 2026, 42));
    }

    [SqlServerFact]
    public async Task AuditLog_FiltersByPersonAreaAndDate_NewestFirst()
    {
        Guid tenant = Guid.NewGuid();
        Guid ada = Guid.NewGuid();
        Guid alan = Guid.NewGuid();

        await using (OperationsDbContext write = _database.NewContext(tenant))
        {
            write.AuditEntries.Add(Entry(tenant, ada, "ops.staff.invited", Now.AddDays(-3)));
            write.AuditEntries.Add(Entry(tenant, ada, "ops.roles.created", Now.AddDays(-2)));
            write.AuditEntries.Add(Entry(tenant, alan, "ops.staff.status_changed", Now.AddDays(-1)));
            write.AuditEntries.Add(Entry(tenant, null, "ops.auth.signed_in", Now));
            await write.SaveChangesAsync();
        }

        await using (OperationsDbContext other = _database.NewContext(Guid.NewGuid()))
        {
            other.AuditEntries.Add(Entry(other.TenantFilterId, ada, "ops.staff.invited", Now));
            await other.SaveChangesAsync();
        }

        await using OperationsDbContext read = _database.NewContext(tenant);
        OperationsAuditRepository reader = new(read);

        (IReadOnlyList<OperationsAuditEntry> all, int total) = await reader.ListAsync(new OperationsAuditFilter(null, null, null, null), 1, 3, CancellationToken.None);
        Assert.Equal(4, total);
        Assert.Equal(new[] { "ops.auth.signed_in", "ops.staff.status_changed", "ops.roles.created" }, all.Select(entry => entry.ActionCode));
        Assert.Equal(2, (await reader.ListAsync(new OperationsAuditFilter(ada, null, null, null), 1, 50, CancellationToken.None)).TotalCount);
        Assert.Equal(2, (await reader.ListAsync(new OperationsAuditFilter(null, "staff", null, null), 1, 50, CancellationToken.None)).TotalCount);
        Assert.Equal("ops.roles.created", Assert.Single((await reader.ListAsync(new OperationsAuditFilter(null, null, Now.AddDays(-2.5), Now.AddDays(-1.5)), 1, 50, CancellationToken.None)).Items).ActionCode);
    }

    private async Task<long> NextAsync(Guid tenant, OperationsNumberKind kind, int year)
    {
        OperationsTenantScope scope = new();
        scope.Enter(tenant);
        await using OperationsDbContext context = _database.NewContext(scope);
        return await new OperationsNumberAllocator(context, scope, TimeProvider.System).NextAsync(kind, year, CancellationToken.None);
    }

    private static OperationsAuditEntry Entry(Guid tenant, Guid? actor, string code, DateTimeOffset at) =>
        OperationsAuditEntry.Record(tenant, new OperationsAuditRecord { ActorUserId = actor, ActionCode = code, TargetType = "OperationsUser" }, at);
}
