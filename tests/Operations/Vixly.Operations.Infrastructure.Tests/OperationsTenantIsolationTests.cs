using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Infrastructure.Persistence;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsTenantIsolationTests.cs
//
// PURPOSE:
// Proves one tenant can neither see nor change another tenant data (Slice 0, D-F1).
//
// RESPONSIBILITIES:
// - Read as tenant A and miss tenant B rows
// - Refuse writes for another tenant
// - Refuse queries with no tenant
//
// IMPORTANT NOTES:
// - Runs against a throwaway SQL Server database
// =========================================================
public sealed class OperationsTenantIsolationTests : IClassFixture<OperationsDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 17, 12, 0, 0, TimeSpan.Zero);

    private readonly OperationsDatabaseFixture _database;

    public OperationsTenantIsolationTests(OperationsDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task TenantA_SeesOnlyItsOwnRows()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        await Add(tenantA, "a-1");
        await Add(tenantB, "b-1");

        await using OperationsDbContext asA = _database.NewContext(tenantA);
        List<string> labels = await asA.Set<FixtureParcel>().Select(parcel => parcel.Label).ToListAsync();

        await using OperationsDbContext asB = _database.NewContext(tenantB);
        List<string> labelsB = await asB.Set<FixtureParcel>().Select(parcel => parcel.Label).ToListAsync();

        Assert.Equal(new[] { "a-1" }, labels);
        Assert.Equal(new[] { "b-1" }, labelsB);
    }

    [SqlServerFact]
    public async Task TenantA_CannotFindTenantB_ById()
    {
        Guid tenantB = Guid.NewGuid();
        Guid id = await Add(tenantB, "b-2");

        await using OperationsDbContext asA = _database.NewContext(Guid.NewGuid());

        Assert.Null(await asA.Set<FixtureParcel>().FirstOrDefaultAsync(parcel => parcel.Id == id));
    }

    [SqlServerFact]
    public async Task TenantA_CannotAddForTenantB()
    {
        await using OperationsDbContext asA = _database.NewContext(Guid.NewGuid());
        asA.Add(new FixtureParcel(Guid.NewGuid(), "smuggled", Now));

        await Assert.ThrowsAsync<InvalidOperationException>(() => asA.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task TenantA_CannotChangeTenantB_EvenWhenAttached()
    {
        Guid tenantB = Guid.NewGuid();
        Guid id = await Add(tenantB, "b-3");
        FixtureParcel parcel;

        await using (OperationsDbContext system = _database.NewSystemContext())
        {
            parcel = await system.Set<FixtureParcel>().AsNoTracking().SingleAsync(item => item.Id == id);
        }

        await using OperationsDbContext asA = _database.NewContext(Guid.NewGuid());
        asA.Attach(parcel);
        parcel.Rename("changed", Now);

        await Assert.ThrowsAsync<InvalidOperationException>(() => asA.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task NoTenant_QueryThrows_InsteadOfReturningEverything()
    {
        await using OperationsDbContext none = _database.NewContext(new OperationsTenantScope());

        await Assert.ThrowsAsync<InvalidOperationException>(() => none.Set<FixtureParcel>().ToListAsync());
    }

    [SqlServerFact]
    public async Task SystemScope_SeesEveryTenant()
    {
        Guid tenantA = Guid.NewGuid();
        Guid tenantB = Guid.NewGuid();
        await Add(tenantA, "s-a");
        await Add(tenantB, "s-b");

        await using OperationsDbContext system = _database.NewSystemContext();

        Assert.Equal(2, await system.Set<FixtureParcel>().CountAsync(parcel => parcel.TenantId == tenantA || parcel.TenantId == tenantB));
    }

    private async Task<Guid> Add(Guid tenantId, string label)
    {
        await using OperationsDbContext context = _database.NewContext(tenantId);
        FixtureParcel parcel = new(tenantId, label, Now);
        context.Add(parcel);
        await context.SaveChangesAsync();
        return parcel.Id;
    }
}
