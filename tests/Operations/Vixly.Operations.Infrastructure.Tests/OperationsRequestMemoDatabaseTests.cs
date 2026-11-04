using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Infrastructure.Persistence.Repositories;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsRequestMemoDatabaseTests.cs
//
// PURPOSE:
// Proves performance plan P-3 against SQL Server: lookups kept for one request never cross companies and are read again after a save.
//
// RESPONSIBILITIES:
// - A company's kept answer is never given to another company or to system work
// - A save, or discarded changes, forgets every kept answer
// - The kept answer is reused within the request
//
// IMPORTANT NOTES:
// - The switching scope below exists only to prove the wall: the real scope refuses to switch (OperationsTenantScope)
// =========================================================
public sealed class OperationsRequestMemoDatabaseTests : IClassFixture<OperationsDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly OperationsDatabaseFixture _database;

    public OperationsRequestMemoDatabaseTests(OperationsDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task KeptLookup_IsNeverGivenToAnotherCompany_OrToSystemWork()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        await AddSettingsAsync(first, "HTG");

        SwitchingScope scope = new() { TenantId = first };
        await using OperationsDbContext context = _database.NewContext(scope);
        OperationsSettingsRepository settings = new(context);

        Assert.Equal("HTG", (await settings.FindAsync(CancellationToken.None))!.CurrencyCode);

        scope.TenantId = second;
        Assert.Null(await settings.FindAsync(CancellationToken.None));

        scope.TenantId = null;
        scope.IsSystem = true;
        Assert.NotNull(await settings.FindAsync(CancellationToken.None));

        scope.IsSystem = false;
        scope.TenantId = second;
        Assert.Null(await settings.FindAsync(CancellationToken.None));
    }

    [SqlServerFact]
    public async Task KeptLookup_IsReused_UntilTheNextSaveOrDiscard()
    {
        Guid tenant = Guid.NewGuid();
        await using OperationsDbContext context = _database.NewContext(tenant);
        OperationsSettingsRepository settings = new(context);

        Assert.Null(await settings.FindAsync(CancellationToken.None));

        // Added elsewhere: this request keeps its first answer...
        await AddSettingsAsync(tenant, "USD");
        Assert.Null(await settings.FindAsync(CancellationToken.None));

        // ...until it saves; then it reads again.
        await context.SaveChangesAsync();
        OperationsTenantSettings found = (await settings.FindAsync(CancellationToken.None))!;
        Assert.Equal("USD", found.CurrencyCode);
        Assert.Same(found, await settings.FindAsync(CancellationToken.None));

        // Discarded changes clear the tracker: the kept row is no longer tracked, so it is read again.
        new OperationsUnitOfWork(context).DiscardChanges();
        Assert.NotSame(found, await settings.FindAsync(CancellationToken.None));
    }

    private async Task AddSettingsAsync(Guid tenant, string currency)
    {
        OperationsTenantSettings settings = OperationsTenantSettings.CreateDefault(tenant, Now);
        settings.ChangeCurrency(currency, false, Now);
        await using OperationsDbContext write = _database.NewContext(tenant);
        new OperationsSettingsRepository(write).Add(settings);
        await write.SaveChangesAsync();
    }

    private sealed class SwitchingScope : IOperationsTenantScope
    {
        public Guid? TenantId { get; set; }

        public bool IsSystem { get; set; }

        public void Enter(Guid tenantId) => TenantId = tenantId;

        public void EnterSystem() => IsSystem = true;
    }
}
