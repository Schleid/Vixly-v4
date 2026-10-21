using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Audit;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsIdentityDatabaseTests.cs
//
// PURPOSE:
// Proves the Slice 1A staff tables against SQL Server.
//
// RESPONSIBILITIES:
// - A user with account, app and codes round-trips
// - Email unique per tenant, the same email in two tenants
// - One Owner and one brand per tenant
// - Sessions and links are invisible to another tenant
//
// IMPORTANT NOTES:
// - Runs against a throwaway SQL Server database
// =========================================================
public sealed class OperationsIdentityDatabaseTests : IClassFixture<OperationsDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly OperationsDatabaseFixture _database;

    public OperationsIdentityDatabaseTests(OperationsDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task Owner_RoundTrips_WithAccountAppAndCodes()
    {
        Guid tenant = Guid.NewGuid();
        OperationsUser owner = Owner(tenant, "grace@northwind.example");
        owner.AcceptInvitation("$2a$12$hash", Now);
        owner.BeginAuthenticatorEnrolment("encrypted", Now);
        owner.ConfirmAuthenticatorApp(5, Now);
        owner.ReplaceRecoveryCodes(Enumerable.Range(0, 10).Select(index => index.ToString("x64", System.Globalization.CultureInfo.InvariantCulture)).ToArray(), Now);
        await Save(tenant, context => context.Users.Add(owner));

        await using OperationsDbContext read = _database.NewContext(tenant);
        OperationsUser? found = await new OperationsUserRepository(read).FindByEmailAsync(EmailAddress.Create("Grace@Northwind.example"), CancellationToken.None);

        Assert.NotNull(found);
        Assert.True(found!.Account.IsActive);
        Assert.True(found.HasSecondFactor);
        Assert.Equal(10, found.UnusedRecoveryCodeCount);
        Assert.Equal(LanguageCode.Fr, found.PreferredLanguage);
    }

    [SqlServerFact]
    public async Task SameEmail_InTwoTenants_IsTwoPeople()
    {
        Guid first = Guid.NewGuid();
        Guid second = Guid.NewGuid();

        await Save(first, context => context.Users.Add(Owner(first, "same@example.com")));
        await Save(second, context => context.Users.Add(Owner(second, "same@example.com")));

        await using OperationsDbContext asFirst = _database.NewContext(first);
        Assert.Single(await asFirst.Users.ToListAsync());
    }

    [SqlServerFact]
    public async Task SecondOwner_InATenant_IsRefused()
    {
        Guid tenant = Guid.NewGuid();
        await Save(tenant, context => context.Users.Add(Owner(tenant, "one@example.com")));

        await Assert.ThrowsAsync<DbUpdateException>(() => Save(tenant, context => context.Users.Add(Owner(tenant, "two@example.com"))));
    }

    [SqlServerFact]
    public async Task SecondBrand_InATenant_IsRefused()
    {
        Guid tenant = Guid.NewGuid();
        await Save(tenant, context => context.TenantBrands.Add(OperationsTenantBrand.Create(tenant, "Northwind", Now)));

        await Assert.ThrowsAsync<DbUpdateException>(() => Save(tenant, context => context.TenantBrands.Add(OperationsTenantBrand.Create(tenant, "Northwind again", Now))));
    }

    [SqlServerFact]
    public async Task SessionsAndLinks_OfAnotherTenant_AreNotFound()
    {
        Guid tenant = Guid.NewGuid();
        OperationsUser owner = Owner(tenant, "owner@example.com");
        string hash = new('c', 64);
        string linkHash = new('d', 64);
        await Save(tenant, context =>
        {
            context.Users.Add(owner);
            context.UserSessions.Add(OperationsUserSession.Start(tenant, owner.Id, hash, null, null, false, Now));
            context.AccountTokens.Add(OperationsAccountToken.Issue(tenant, owner.Id, OperationsAccountTokenPurpose.Invitation, linkHash, Now));
            context.AuditEntries.Add(OperationsAuditEntry.Record(tenant, new OperationsAuditRecord { ActionCode = "ops.auth.signed_in", TargetType = "OperationsUser" }, Now));
        });

        await using OperationsDbContext other = _database.NewContext(Guid.NewGuid());
        Assert.Null(await new OperationsSessionRepository(other).FindByTokenHashAsync(hash, CancellationToken.None));
        Assert.Null(await new OperationsAccountTokenRepository(other).FindByTokenHashAsync(linkHash, CancellationToken.None));
        Assert.Empty(await other.AuditEntries.ToListAsync());

        await using OperationsDbContext own = _database.NewContext(tenant);
        Assert.NotNull(await new OperationsSessionRepository(own).FindByTokenHashAsync(hash, CancellationToken.None));
    }

    private static OperationsUser Owner(Guid tenant, string email) =>
        OperationsUser.InviteOwner(tenant, "Grace Hopper", EmailAddress.Create(email), LanguageCode.Fr, Now);

    private async Task Save(Guid tenant, Action<OperationsDbContext> change)
    {
        await using OperationsDbContext context = _database.NewContext(tenant);
        change(context);
        await context.SaveChangesAsync();
    }
}
