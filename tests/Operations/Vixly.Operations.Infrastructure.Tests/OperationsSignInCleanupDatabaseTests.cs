using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsSignInCleanupDatabaseTests.cs
//
// PURPOSE:
// Expired sign-in rows are removed across every company, and
// nothing still useful is (background jobs comparison,
// 2026-10-22).
//
// RESPONSIBILITIES:
// - Sessions a year after they expired, links 30 days after
//
// IMPORTANT NOTES:
// - Dated in 2019 and 2020 so no other test's rows are old
//   enough to be touched
// =========================================================
public sealed class OperationsSignInCleanupDatabaseTests : IClassFixture<OperationsDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2021, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly OperationsDatabaseFixture _database;

    public OperationsSignInCleanupDatabaseTests(OperationsDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task ExpiredSessionsAndLinks_AreRemoved_InEveryCompany_RecentOnesStay()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();
        OperationsUser one = OperationsUser.InviteOwner(first, "Grace Hopper", EmailAddress.Create(Guid.NewGuid().ToString("N")[..12] + "@one.example"), LanguageCode.En, Now.AddYears(-2));
        OperationsUser two = OperationsUser.InviteOwner(second, "Ada Lovelace", EmailAddress.Create(Guid.NewGuid().ToString("N")[..12] + "@two.example"), LanguageCode.En, Now.AddYears(-2));
        OperationsUserSession oldSession = OperationsUserSession.Start(first, one.Id, Hash(), null, null, false, Now.AddDays(-400));
        OperationsUserSession yearOld = OperationsUserSession.Start(second, two.Id, Hash(), null, null, false, Now.AddDays(-300));
        OperationsAccountToken oldLink = OperationsAccountToken.Issue(second, two.Id, OperationsAccountTokenPurpose.PasswordReset, Hash(), Now.AddDays(-40));
        OperationsAccountToken freshLink = OperationsAccountToken.Issue(first, one.Id, OperationsAccountTokenPurpose.PasswordReset, Hash(), Now.AddDays(-1));
        OperationsSignInChallenge oldChallenge = OperationsSignInChallenge.Issue(first, one.Id, Hash(), OperationsSignInStage.SecondFactor, Now.AddDays(-31));

        await Save(first, context =>
        {
            context.Users.Add(one);
            context.UserSessions.Add(oldSession);
            context.AccountTokens.Add(freshLink);
            context.SignInChallenges.Add(oldChallenge);
        });
        await Save(second, context =>
        {
            context.Users.Add(two);
            context.UserSessions.Add(yearOld);
            context.AccountTokens.Add(oldLink);
        });

        await using (OperationsDbContext system = _database.NewSystemContext())
        {
            Assert.Equal(3, await new OperationsSignInCleanup(new OperationsSignInCleanupRepository(system)).RunAsync(Now, CancellationToken.None));
        }

        await using OperationsDbContext check = _database.NewSystemContext();
        Assert.False(await check.UserSessions.AnyAsync(item => item.Id == oldSession.Id));
        Assert.True(await check.UserSessions.AnyAsync(item => item.Id == yearOld.Id));
        Assert.False(await check.AccountTokens.AnyAsync(item => item.Id == oldLink.Id));
        Assert.True(await check.AccountTokens.AnyAsync(item => item.Id == freshLink.Id));
        Assert.False(await check.SignInChallenges.AnyAsync(item => item.Id == oldChallenge.Id));

        await using (OperationsDbContext again = _database.NewSystemContext())
        {
            Assert.Equal(0, await new OperationsSignInCleanup(new OperationsSignInCleanupRepository(again)).RunAsync(Now, CancellationToken.None));
        }
    }

    private static string Hash() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");

    private async Task Save(Guid tenant, Action<OperationsDbContext> change)
    {
        await using OperationsDbContext context = _database.NewContext(tenant);
        change(context);
        await context.SaveChangesAsync();
    }
}
