using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Repositories;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformEmployeeNumberDatabaseTests.cs
//
// PURPOSE:
// Proves Control staff employee numbers come from the database sequence (owner, 2026-10-25).
//
// RESPONSIBILITIES:
// - Each call gives a new TC- number, one higher than the last, even from separate contexts
//
// IMPORTANT NOTES:
// - A plain command, not SqlQueryRaw: SQL Server refuses NEXT VALUE FOR inside EF's subquery
// =========================================================
public sealed class PlatformEmployeeNumberDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private readonly PlatformDatabaseFixture _database;

    public PlatformEmployeeNumberDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task NextEmployeeNumber_IsUnique_AndIncreasing()
    {
        string first;
        string second;

        await using (PlatformDbContext context = _database.NewContext())
        {
            first = await new PlatformUserRepository(context).NextEmployeeNumberAsync(CancellationToken.None);
        }

        await using (PlatformDbContext context = _database.NewContext())
        {
            second = await new PlatformUserRepository(context).NextEmployeeNumberAsync(CancellationToken.None);
        }

        Assert.StartsWith("TC-", first, StringComparison.Ordinal);
        Assert.Equal(long.Parse(first[3..], System.Globalization.CultureInfo.InvariantCulture) + 1, long.Parse(second[3..], System.Globalization.CultureInfo.InvariantCulture));
    }
}
