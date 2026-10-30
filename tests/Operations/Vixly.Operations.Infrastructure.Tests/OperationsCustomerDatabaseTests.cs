using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Infrastructure.Persistence;
using Vixly.Operations.Infrastructure.Persistence.Repositories;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Infrastructure.Tests;

// =========================================================
// FILE: OperationsCustomerDatabaseTests.cs
//
// PURPOSE:
// Proves the customer tables of Slice 2 against SQL Server.
//
// RESPONSIBILITIES:
// - Customers, sessions and links round-trip and stay in their tenant
// - One email per tenant; the same email in two tenants
// - Search by name, email, phone or code, paged
//
// IMPORTANT NOTES:
// - Runs against a throwaway SQL Server database
// =========================================================
public sealed class OperationsCustomerDatabaseTests : IClassFixture<OperationsDatabaseFixture>
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    private static readonly string Hash = new('b', 64);

    private readonly OperationsDatabaseFixture _database;

    public OperationsCustomerDatabaseTests(OperationsDatabaseFixture database)
    {
        _database = database;
    }

    [SqlServerFact]
    public async Task Customer_SessionAndLink_RoundTrip_AndStayInTheirTenant()
    {
        Guid tenant = Guid.NewGuid();
        OperationsCustomer customer = Customer(tenant, "ana@example.com", "C00001");
        string sessionHash = new('c', 64);

        await using (OperationsDbContext write = _database.NewContext(tenant))
        {
            OperationsCustomerRepository customers = new(write, TimeProvider.System);
            customers.Add(customer);
            customers.Add(OperationsCustomerSession.Start(tenant, customer.Id, sessionHash, "203.0.113.5", "Browser", Now));
            customers.Add(OperationsCustomerToken.Issue(tenant, customer.Id, OperationsCustomerTokenPurpose.PasswordReset, Hash, Now));
            await write.SaveChangesAsync();
        }

        await using OperationsDbContext read = _database.NewContext(tenant);
        OperationsCustomerRepository again = new(read, TimeProvider.System);
        OperationsCustomer found = (await again.FindByEmailAsync(EmailAddress.Create("ANA@example.com"), CancellationToken.None))!;
        Assert.Equal("C00001", found.Code);
        Assert.Equal("+50937123456", found.Phone);
        Assert.Equal(customer.Id, (await again.FindSessionAsync(sessionHash, CancellationToken.None))!.CustomerId);
        Assert.Single(await again.ListOpenSessionsAsync(customer.Id, CancellationToken.None));
        Assert.Single(await again.ListOpenTokensAsync(customer.Id, OperationsCustomerTokenPurpose.PasswordReset, CancellationToken.None));
        Assert.Empty(await again.ListOpenTokensAsync(customer.Id, OperationsCustomerTokenPurpose.Verification, CancellationToken.None));

        await using OperationsDbContext other = _database.NewContext(Guid.NewGuid());
        OperationsCustomerRepository elsewhere = new(other, TimeProvider.System);
        Assert.Null(await elsewhere.GetAsync(customer.Id, CancellationToken.None));
        Assert.Null(await elsewhere.FindSessionAsync(sessionHash, CancellationToken.None));
        Assert.Null(await elsewhere.FindTokenAsync(Hash, CancellationToken.None));
    }

    [SqlServerFact]
    public async Task Email_IsUniquePerTenant_ButMayExistInTwoTenants()
    {
        Guid north = Guid.NewGuid();
        Guid south = Guid.NewGuid();

        await SaveAsync(north, Customer(north, "same@example.com", "C00001"));
        await SaveAsync(south, Customer(south, "same@example.com", "C00001"));

        await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(north, Customer(north, "same@example.com", "C00002")));
        await Assert.ThrowsAsync<DbUpdateException>(() => SaveAsync(north, Customer(north, "other@example.com", "C00001")));
    }

    [SqlServerFact]
    public async Task Search_FindsByNameEmailPhoneOrCode_Paged()
    {
        Guid tenant = Guid.NewGuid();

        for (int number = 1; number <= 30; number++)
        {
            await SaveAsync(tenant, Customer(tenant, $"person{number}@example.com", "C" + number.ToString("00000"), number == 7 ? "Zora" : "Ana"));
        }

        await using OperationsDbContext read = _database.NewContext(tenant);
        OperationsCustomerRepository customers = new(read, TimeProvider.System);

        (IReadOnlyList<OperationsCustomer> first, int total) = await customers.ListAsync(null, 1, 25, CancellationToken.None);
        (IReadOnlyList<OperationsCustomer> second, int _) = await customers.ListAsync(null, 2, 25, CancellationToken.None);
        Assert.Equal(30, total);
        Assert.Equal(25, first.Count);
        Assert.Equal(5, second.Count);
        Assert.Equal("Zora", Assert.Single((await customers.ListAsync("zora", 1, 25, CancellationToken.None)).Items).FirstName);
        Assert.Equal("C00012", Assert.Single((await customers.ListAsync("C00012", 1, 25, CancellationToken.None)).Items).Code);
        Assert.Equal("person3@example.com", Assert.Single((await customers.ListAsync("person3@example.com", 1, 25, CancellationToken.None)).Items).Email.Value);
        Assert.Equal(30, (await customers.ListAsync("+50937123456", 1, 25, CancellationToken.None)).TotalCount);
    }

    [SqlServerFact]
    public async Task FailedSignIns_SentAtTheSameTime_AllCount()
    {
        Guid tenant = Guid.NewGuid();
        OperationsCustomer customer = Customer(tenant, "race@example.com", "C00001");
        await SaveAsync(tenant, customer);

        bool[] locked = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => Task.Run(async () =>
        {
            await using OperationsDbContext context = _database.NewContext(tenant);
            return await new OperationsCustomerRepository(context, TimeProvider.System).RecordFailedSignInAsync(customer.Id, Now, CancellationToken.None);
        })));

        Assert.Contains(true, locked);
        await using OperationsDbContext read = _database.NewContext(tenant);
        OperationsCustomer found = (await new OperationsCustomerRepository(read, TimeProvider.System).GetAsync(customer.Id, CancellationToken.None))!;
        Assert.True(found.IsLockedOut(Now.AddMinutes(14)));
        Assert.Equal(0, found.FailedSignInCount);

        await using OperationsDbContext other = _database.NewContext(Guid.NewGuid());
        Assert.False(await new OperationsCustomerRepository(other, TimeProvider.System).RecordFailedSignInAsync(customer.Id, Now, CancellationToken.None));
    }

    private async Task SaveAsync(Guid tenant, OperationsCustomer customer)
    {
        await using OperationsDbContext write = _database.NewContext(tenant);
        new OperationsCustomerRepository(write, TimeProvider.System).Add(customer);
        await write.SaveChangesAsync();
    }

    private static OperationsCustomer Customer(Guid tenant, string email, string code, string firstName = "Ana")
    {
        OperationsCustomer customer = OperationsCustomer.SignUp(tenant, firstName, "Pierre", null, EmailAddress.Create(email), "+50937123456", LanguageCode.En, "$2a$12$hash", Now);
        customer.ConfirmEmail(code, Now);
        return customer;
    }
}
