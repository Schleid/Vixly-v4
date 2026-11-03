using Microsoft.EntityFrameworkCore;
using Vixly.Platform.Application.Identity;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Repositories;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformSignInCleanupDatabaseTests.cs
//
// PURPOSE:
// Expired Control sign-in rows are removed, recent ones stay
// (background jobs comparison, 2026-10-22).
//
// RESPONSIBILITIES:
// - Sessions a year after they expired, links 30 days after
//
// IMPORTANT NOTES:
// - Dated in 2019 and 2020 so no other test's rows are old
//   enough to be touched
// =========================================================
public sealed class PlatformSignInCleanupDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2021, 1, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly PlatformDatabaseFixture _database;

    public PlatformSignInCleanupDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task ExpiredSessionsAndLinks_AreRemoved_RecentOnesStay()
    {
        Guid user = _database.ActorUserId;
        PlatformUserSession oldSession = PlatformUserSession.Start(user, Hash(), null, null, Now.AddDays(-400));
        PlatformUserSession recentSession = PlatformUserSession.Start(user, Hash(), null, null, Now.AddDays(-300));
        PlatformAccountToken oldLink = PlatformAccountToken.Issue(user, PlatformAccountTokenPurpose.PasswordReset, Hash(), Now.AddDays(-40));
        PlatformAccountToken freshLink = PlatformAccountToken.Issue(user, PlatformAccountTokenPurpose.PasswordReset, Hash(), Now.AddDays(-1));
        PlatformSignInChallenge oldChallenge = PlatformSignInChallenge.Issue(user, Hash(), PlatformSignInStage.SecondFactor, Now.AddDays(-31));

        await using (PlatformDbContext context = _database.NewContext())
        {
            context.UserSessions.AddRange(oldSession, recentSession);
            context.AccountTokens.AddRange(oldLink, freshLink);
            context.SignInChallenges.Add(oldChallenge);
            await context.SaveChangesAsync();
        }

        FixedTimeProvider clock = new(Now);

        await using (PlatformDbContext context = _database.NewContext())
        {
            Assert.Equal(3, await new PlatformSignInCleanupClock(new PlatformSignInCleanupRepository(context), clock).RunAsync(CancellationToken.None));
        }

        await using PlatformDbContext check = _database.NewContext();
        Assert.False(await check.UserSessions.AnyAsync(item => item.Id == oldSession.Id));
        Assert.True(await check.UserSessions.AnyAsync(item => item.Id == recentSession.Id));
        Assert.False(await check.AccountTokens.AnyAsync(item => item.Id == oldLink.Id));
        Assert.True(await check.AccountTokens.AnyAsync(item => item.Id == freshLink.Id));
        Assert.False(await check.SignInChallenges.AnyAsync(item => item.Id == oldChallenge.Id));
    }

    private static string Hash() => Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
}
