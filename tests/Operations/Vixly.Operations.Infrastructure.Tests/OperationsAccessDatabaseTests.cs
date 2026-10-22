using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsAccessDatabaseTests.cs
//
// PURPOSE:
// Proves the Slice 1D role, grant and staff tables against SQL Server.
//
// RESPONSIBILITIES:
// - Roles and grants round-trip and stay in their tenant
// - One Administrator role and unique role names per tenant
// - Staff are listed Owner first, with search and paging; a status reason round-trips
//
// IMPORTANT NOTES:
// - Runs against a throwaway SQL Server database
// =========================================================
public sealed class OperationsAccessDatabaseTests : IClassFixture<OperationsDatabaseFixture>
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);

    private readonly OperationsDatabaseFixture _database;

    public OperationsAccessDatabaseTests(OperationsDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task RolesAndGrants_RoundTrip_AndStayInTheirTenant()
    {
        Guid tenant = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        Guid warehouse = Guid.NewGuid();
        OperationsRole role = OperationsRole.Create(tenant, "Receiving clerk", new[] { OperationsPermissions.WarehousesView, OperationsPermissions.StaffView }, Now);
        OperationsRole administrator = OperationsRole.CreateAdministrator(tenant, Now);
        OperationsUser user = Staff(tenant, "ada@northwind.example", "Ada Lovelace");
        OperationsRoleGrant grant = OperationsRoleGrant.Grant(tenant, user.Id, role.Id, OperationsRoleScope.Warehouse, warehouse, null, Now);
        OperationsRoleGrant revoked = OperationsRoleGrant.Grant(tenant, user.Id, administrator.Id, OperationsRoleScope.Tenant, null, null, Now);
        revoked.Revoke(null, Now);

        await Save(tenant, context =>
        {
            context.Users.Add(user);
            context.Roles.AddRange(role, administrator);
            context.RoleGrants.AddRange(grant, revoked);
        });
        await Save(other, context => context.Roles.Add(OperationsRole.Create(other, "Receiving clerk", Array.Empty<string>(), Now)));

        await using OperationsDbContext read = _database.NewContext(tenant);
        OperationsRoleRepository roles = new(read);
        OperationsRole found = (await roles.GetAsync(role.Id, CancellationToken.None))!;
        Assert.Equal(new[] { OperationsPermissions.StaffView, OperationsPermissions.WarehousesView }, found.Permissions);
        Assert.True((await roles.FindAdministratorAsync(CancellationToken.None))!.IsAdministrator);
        Assert.Equal(2, (await roles.ListAsync(CancellationToken.None)).Count);
        OperationsRoleGrant active = Assert.Single(await roles.ListActiveGrantsAsync(CancellationToken.None));
        Assert.Equal(OperationsRoleScope.Warehouse, active.Scope);
        Assert.Equal(warehouse, active.ScopeId);
        Assert.Equal(2, (await roles.ListGrantsAsync(user.Id, CancellationToken.None)).Count);

        await using OperationsDbContext asOther = _database.NewContext(other);
        Assert.Null(await new OperationsRoleRepository(asOther).GetAsync(role.Id, CancellationToken.None));
        Assert.Empty(await new OperationsRoleRepository(asOther).ListActiveGrantsAsync(CancellationToken.None));
    }

    [SqlServerFact]
    public async Task OneAdministratorRole_AndUniqueNames_PerTenant()
    {
        Guid tenant = Guid.NewGuid();
        await Save(tenant, context => context.Roles.AddRange(OperationsRole.CreateAdministrator(tenant, Now), OperationsRole.Create(tenant, "Driver", Array.Empty<string>(), Now)));

        await Assert.ThrowsAsync<DbUpdateException>(() => Save(tenant, context => context.Roles.Add(OperationsRole.CreateAdministrator(tenant, Now))));
        await Assert.ThrowsAsync<DbUpdateException>(() => Save(tenant, context => context.Roles.Add(OperationsRole.Create(tenant, "Driver", Array.Empty<string>(), Now))));
    }

    [SqlServerFact]
    public async Task Staff_AreListedOwnerFirst_WithSearchAndPaging()
    {
        Guid tenant = Guid.NewGuid();
        OperationsUser owner = OperationsUser.InviteOwner(tenant, "Zora Neale", EmailAddress.Create("zora@northwind.example"), LanguageCode.En, Now);
        OperationsUser ada = Staff(tenant, "ada@northwind.example", "Ada Lovelace");
        OperationsUser alan = Staff(tenant, "alan@northwind.example", "Alan Turing");
        alan.ChangeStatus(OperationsUserAccountStatus.Suspended, "Investigation", Now);
        await Save(tenant, context => context.Users.AddRange(owner, ada, alan));

        await using OperationsDbContext read = _database.NewContext(tenant);
        OperationsUserRepository users = new(read);

        (IReadOnlyList<OperationsUser> all, int total) = await users.ListAsync(null, 1, 2, CancellationToken.None);
        Assert.Equal(3, total);
        Assert.Equal(new[] { "Zora Neale", "Ada Lovelace" }, all.Select(user => user.FullName));
        Assert.Equal("Alan Turing", Assert.Single((await users.ListAsync(null, 2, 2, CancellationToken.None)).Items).FullName);
        Assert.Equal("Alan Turing", Assert.Single((await users.ListAsync("Turing", 1, 25, CancellationToken.None)).Items).FullName);
        OperationsUser byEmail = Assert.Single((await users.ListAsync("ADA@northwind.example", 1, 25, CancellationToken.None)).Items);
        Assert.Equal("Ada Lovelace", byEmail.FullName);

        OperationsUser suspended = (await users.GetAsync(alan.Id, CancellationToken.None))!;
        Assert.Equal(OperationsUserAccountStatus.Suspended, suspended.Account.Status);
        Assert.Equal("Investigation", suspended.Account.StatusReason);
        Assert.Equal(Now, suspended.Account.StatusChangedUtc);
    }

    private static OperationsUser Staff(Guid tenant, string email, string name)
    {
        OperationsUser user = OperationsUser.InviteStaff(tenant, name, EmailAddress.Create(email), LanguageCode.En, Now);
        user.AcceptInvitation("hash", Now);
        return user;
    }

    private async Task Save(Guid tenant, Action<OperationsDbContext> change)
    {
        await using OperationsDbContext context = _database.NewContext(tenant);
        change(context);
        await context.SaveChangesAsync();
    }
}
