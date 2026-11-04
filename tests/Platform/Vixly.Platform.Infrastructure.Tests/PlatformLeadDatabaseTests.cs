using Vixly.Platform.Application.Queries;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformLeadDatabaseTests.cs
//
// PURPOSE:
// Proves sales leads on SQL Server (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - A lead round-trips with every field
// - The console list filters by status and search, newest first, and names who handled a lead
// - Closed leads past the retention are deleted; open ones stay
//
// IMPORTANT NOTES:
// - Runs only when VIXLY_TEST_SQL_SERVER is set (SqlServerFact)
// - Each test uses its own company names: the database is shared
// =========================================================
public sealed class PlatformLeadDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 16, 12, 0, 0, TimeSpan.Zero);

    private readonly PlatformDatabaseFixture _database;

    public PlatformLeadDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    private static PlatformSalesLead Lead(string company, DateTimeOffset at, string? message = null)
    {
        return PlatformSalesLead.Create(company, "Ana Rivera", "ana@rivera.example", "+1 305 555 0100", "US", PlatformSalesLeadKind.MultiBranch, message, LanguageCode.Es, "contact", "198.51.100.4", at);
    }

    private async Task SaveAsync(params PlatformSalesLead[] leads)
    {
        await using PlatformDbContext context = _database.NewContext();
        PlatformSalesLeadRepository repository = new(context);

        foreach (PlatformSalesLead lead in leads)
        {
            repository.Add(lead);
        }

        Assert.True(await new PlatformUnitOfWork(context).SaveChangesAsync(CancellationToken.None));
    }

    [SqlServerFact]
    public async Task Lead_RoundTrips_WithEveryField()
    {
        string company = "RoundTrip " + Guid.NewGuid().ToString("N")[..8];
        PlatformSalesLead lead = Lead(company, Now, "Three branches,\nsix vans.");
        await SaveAsync(lead);

        await using PlatformDbContext context = _database.NewContext();
        PlatformSalesLeadItem item = (await new PlatformSalesLeadRepository(context).GetItemAsync(lead.Id, CancellationToken.None))!;

        Assert.Equal(company, item.CompanyName);
        Assert.Equal("+13055550100", item.Phone);
        Assert.Equal("US", item.CountryCode);
        Assert.Equal(PlatformSalesLeadKind.MultiBranch, item.Kind);
        Assert.Equal("Three branches,\nsix vans.", item.Message);
        Assert.Equal("es", item.Language);
        Assert.Equal(PlatformSalesLeadStatus.New, item.Status);
        Assert.Equal(Now, item.ReceivedUtc);
        Assert.Null(item.HandledByName);
    }

    [SqlServerFact]
    public async Task List_FiltersByStatusAndSearch_NewestFirst()
    {
        string tag = Guid.NewGuid().ToString("N")[..8];
        PlatformSalesLead older = Lead("Older " + tag, Now.AddHours(-2));
        PlatformSalesLead newer = Lead("Newer " + tag, Now.AddHours(-1));
        PlatformSalesLead closed = Lead("Closed " + tag, Now);
        closed.ChangeStatus(PlatformSalesLeadStatus.Closed, "No fit", _database.ActorUserId, Now);
        await SaveAsync(older, newer, closed);

        await using PlatformDbContext context = _database.NewContext();
        PlatformSalesLeadRepository repository = new(context);
        PagedResult<PlatformSalesLeadItem> open = await repository.ListAsync(new PlatformSalesLeadFilter(PlatformSalesLeadStatus.New, tag), PagedRequest.First, CancellationToken.None);
        PagedResult<PlatformSalesLeadItem> all = await repository.ListAsync(new PlatformSalesLeadFilter(null, tag), PagedRequest.First, CancellationToken.None);

        Assert.Equal(new[] { "Newer " + tag, "Older " + tag }, open.Items.Select(item => item.CompanyName));
        Assert.Equal(3, all.TotalCount);
        Assert.Equal("Closed " + tag, all.Items[0].CompanyName);
        Assert.Equal("Test Actor", all.Items[0].HandledByName);
    }

    [SqlServerFact]
    public async Task Retention_DeletesClosedLeadsPastTheCutoff_Only()
    {
        string tag = Guid.NewGuid().ToString("N")[..8];
        DateTimeOffset longAgo = new(2020, 1, 1, 0, 0, 0, TimeSpan.Zero);
        PlatformSalesLead oldClosed = Lead("OldClosed " + tag, longAgo);
        oldClosed.ChangeStatus(PlatformSalesLeadStatus.Closed, null, _database.ActorUserId, longAgo);
        PlatformSalesLead oldOpen = Lead("OldOpen " + tag, longAgo);
        PlatformSalesLead recentClosed = Lead("RecentClosed " + tag, Now);
        recentClosed.ChangeStatus(PlatformSalesLeadStatus.Closed, null, _database.ActorUserId, Now);
        await SaveAsync(oldClosed, oldOpen, recentClosed);

        await using PlatformDbContext context = _database.NewContext();
        PlatformSalesLeadRepository repository = new(context);
        int removed = await repository.DeleteClosedBeforeAsync(Now - PlatformSalesLeadLimits.ClosedRetention, CancellationToken.None);

        Assert.True(removed >= 1);
        Assert.Null(await repository.GetItemAsync(oldClosed.Id, CancellationToken.None));
        Assert.NotNull(await repository.GetItemAsync(oldOpen.Id, CancellationToken.None));
        Assert.NotNull(await repository.GetItemAsync(recentClosed.Id, CancellationToken.None));
    }
}
