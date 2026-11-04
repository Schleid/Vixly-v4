using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Tests.Fakes;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsBannedContactsTests.cs
//
// PURPOSE:
// Checks that a banned customer's email and phone cannot open another account (owner, 2026-10-31).
//
// RESPONSIBILITIES:
// - The email of a banned customer, and their phone written any way, are recognised
// - A suspended or closed customer's contacts are not
//
// IMPORTANT NOTES:
// - The pages are checked in the Web tests (OperationsCustomerHttpTests)
// =========================================================
public sealed class OperationsBannedContactsTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 31, 15, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task BannedEmailAndPhone_AreRecognised_OthersAreNot()
    {
        FakeCustomerStore store = new();
        OperationsCustomer banned = Customer("rex@example.com", "+50937001122");
        banned.ChangeStatus(OperationsCustomerStatus.Banned, "Fraud", Now);
        OperationsCustomer closed = Customer("ana@example.com", "+50937003344");
        closed.ChangeStatus(OperationsCustomerStatus.Closed, "Moved away", Now);
        store.Customers.Add(banned);
        store.Customers.Add(closed);
        OperationsBannedContacts contacts = new(store);

        Assert.True(await contacts.IsBannedAsync(EmailAddress.Create("REX@example.com"), null, CancellationToken.None));
        Assert.True(await contacts.IsBannedAsync(null, "+509 3700-1122", CancellationToken.None));
        Assert.False(await contacts.IsBannedAsync(EmailAddress.Create("ana@example.com"), "+50937003344", CancellationToken.None));
        Assert.False(await contacts.IsBannedAsync(null, "112", CancellationToken.None));
    }

    private static OperationsCustomer Customer(string email, string phone)
    {
        OperationsCustomer customer = OperationsCustomer.SignUp(Guid.NewGuid(), "Rex", "Paul", null, EmailAddress.Create(email), phone, LanguageCode.En, "$2a$12$hash", Now);
        customer.ConfirmEmail("C" + Random.Shared.Next(10000, 99999), Now);
        return customer;
    }
}
