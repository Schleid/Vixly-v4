using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Tests.Fakes;

// =========================================================
// FILE: FakeCustomerStore.cs
//
// PURPOSE:
// An in-memory store of customers, for the staff tests that ask whether an email is a customer's (review item 3).
//
// RESPONSIBILITIES:
// - Hold what a test adds; answer by email
//
// IMPORTANT NOTES:
// - Tests only; what the staff tests never call is not supported
// =========================================================
internal sealed class FakeCustomerStore : IOperationsCustomerRepository
{
    public List<OperationsCustomer> Customers { get; } = new();

    public void Add(OperationsCustomer customer) => Customers.Add(customer);

    public void Add(OperationsCustomerSession session) => throw new NotSupportedException();

    public void Add(OperationsCustomerToken token) => throw new NotSupportedException();

    public Task<OperationsCustomer?> GetAsync(Guid customerId, CancellationToken cancellationToken) => Task.FromResult(Customers.FirstOrDefault(customer => customer.Id == customerId));

    public Task<OperationsCustomer?> FindByCodeAsync(string code, CancellationToken cancellationToken) => Task.FromResult(Customers.FirstOrDefault(customer => customer.Code == code));

    public Task<IReadOnlyList<OperationsCustomer>> ListByIdsAsync(IReadOnlyCollection<Guid> customerIds, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<OperationsCustomer>>(Customers.Where(customer => customerIds.Contains(customer.Id)).ToList());

    public Task<OperationsCustomer?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken) => Task.FromResult(Customers.FirstOrDefault(customer => customer.Email == email));

    public Task<bool> IsCustomerEmailAnywhereAsync(EmailAddress email, CancellationToken cancellationToken) =>
        Task.FromResult(Customers.Any(customer => customer.Email == email && customer.EmailVerifiedUtc is not null && customer.Status != OperationsCustomerStatus.Closed));

    public Task<OperationsCustomerSession?> FindSessionAsync(string tokenHash, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<OperationsCustomerSession>> ListOpenSessionsAsync(Guid customerId, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<OperationsCustomerToken?> FindTokenAsync(string tokenHash, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<IReadOnlyList<OperationsCustomerToken>> ListOpenTokensAsync(Guid customerId, OperationsCustomerTokenPurpose purpose, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<bool> RecordFailedSignInAsync(Guid customerId, DateTimeOffset utcNow, CancellationToken cancellationToken) => throw new NotSupportedException();

    public Task<(IReadOnlyList<OperationsCustomer> Items, int TotalCount)> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<OperationsCustomer>> FindForCounterAsync(string text, int take, CancellationToken cancellationToken) => throw new NotSupportedException();
}
