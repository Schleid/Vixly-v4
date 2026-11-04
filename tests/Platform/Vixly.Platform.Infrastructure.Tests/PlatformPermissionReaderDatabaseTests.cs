using Vixly.Platform.Application.Identity;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Infrastructure.Persistence;
using Vixly.Platform.Infrastructure.Persistence.Repositories;
using Vixly.Platform.Infrastructure.Security;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Infrastructure.Tests;

// =========================================================
// FILE: PlatformPermissionReaderDatabaseTests.cs
//
// PURPOSE:
// Proves Control's harmonization against SQL Server (owner, 2026-11-04): one permission read per action (H-9) and the small session read (H-10).
//
// RESPONSIBILITIES:
// - The same action reuses its answer; a new action, a save or 5 seconds read again
// - A right removed elsewhere applies from the next action
// - The session facts are the staff member's own: active state, level, name, language
//
// IMPORTANT NOTES:
// - A new action is what PlatformUseCaseScope.Begin does: discard the changes
// =========================================================
public sealed class PlatformPermissionReaderDatabaseTests : IClassFixture<PlatformDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 11, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly PlatformDatabaseFixture _database;

    public PlatformPermissionReaderDatabaseTests(PlatformDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task OneReadPerAction_AndARemovedRightAppliesFromTheNextAction()
    {
        string code = PlatformPermissions.All.Order(StringComparer.Ordinal).First();
        PlatformRole role = PlatformRole.CreateCustom("Reader test " + Guid.NewGuid().ToString("N")[..8], Now);
        role.ReplacePermissions(new[] { code }, Now);
        PlatformUser user = PlatformUser.Invite("Ada", "Lovelace", EmailAddress.Create($"ada-{Guid.NewGuid():N}@techcrafters.example"), LanguageCode.En,
            PlatformUserAuthorityLevel.Standard, null, null, Now);
        user.AssignRole(role.Id, _database.ActorUserId, null, Now);

        await using (PlatformDbContext write = _database.NewContext())
        {
            write.Roles.Add(role);
            write.Users.Add(user);
            await write.SaveChangesAsync();
        }

        await using PlatformDbContext page = _database.NewContext();
        PlatformPermissionReader reader = new(page);
        PlatformPermissionSnapshot first = (await reader.ReadAsync(user.Id, Now, CancellationToken.None))!;
        Assert.Contains(code, first.PermissionCodes);

        // The right is removed elsewhere while this action runs: the action keeps the answer it started with.
        await using (PlatformDbContext other = _database.NewContext())
        {
            PlatformUser stored = (await new PlatformUserRepository(other).GetAsync(user.Id, CancellationToken.None))!;
            stored.RevokeRole(role.Id, _database.ActorUserId, Now.AddSeconds(1));
            await other.SaveChangesAsync();
        }

        Assert.Same(first, await reader.ReadAsync(user.Id, Now.AddSeconds(1), CancellationToken.None));

        // The next action reads again and no longer has it.
        new PlatformUnitOfWork(page).DiscardChanges();
        PlatformPermissionSnapshot next = (await reader.ReadAsync(user.Id, Now.AddSeconds(2), CancellationToken.None))!;
        Assert.DoesNotContain(code, next.PermissionCodes);

        // Never kept past 5 seconds, nor past a save, nor for another person.
        Assert.Same(next, await reader.ReadAsync(user.Id, Now.AddSeconds(3), CancellationToken.None));
        Assert.NotSame(next, await reader.ReadAsync(user.Id, Now.AddSeconds(8), CancellationToken.None));
        PlatformPermissionSnapshot afterTime = (await reader.ReadAsync(user.Id, Now.AddSeconds(8), CancellationToken.None))!;
        await page.SaveChangesAsync();
        Assert.NotSame(afterTime, await reader.ReadAsync(user.Id, Now.AddSeconds(8), CancellationToken.None));
        Assert.NotEqual(PlatformUserAuthorityLevel.Standard, (await reader.ReadAsync(_database.ActorUserId, Now.AddSeconds(8), CancellationToken.None))!.AuthorityLevel);

        // H-10: the small session read gives the same facts as the whole staff member.
        PlatformSessionFacts facts = (await new PlatformUserRepository(page).FindSessionFactsAsync(user.Id, CancellationToken.None))!;
        PlatformUser whole = (await new PlatformUserRepository(page).GetAsync(user.Id, CancellationToken.None))!;
        Assert.Equal((whole.Id, whole.Account.IsActive, whole.AuthorityLevel, whole.FirstName, whole.LastName, whole.PreferredLanguage.Value),
            (facts.PlatformUserId, facts.IsActive, facts.AuthorityLevel, facts.FirstName, facts.LastName, facts.PreferredLanguage));
        Assert.Null(await new PlatformUserRepository(page).FindSessionFactsAsync(Guid.NewGuid(), CancellationToken.None));
    }
}
