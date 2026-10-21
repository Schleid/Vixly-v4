using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsCustomerRepository.cs
//
// PURPOSE:
// Stores the tenant's customers, their sessions and links (Slice 2).
//
// RESPONSIBILITIES:
// - Find customers by id, email or session; list them with search and paging
// - Add sessions and single-use links
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1); the same email may exist in another tenant
// =========================================================
public interface IOperationsCustomerRepository
{
    void Add(OperationsCustomer customer);

    void Add(OperationsCustomerSession session);

    void Add(OperationsCustomerToken token);

    Task<OperationsCustomer?> GetAsync(Guid customerId, CancellationToken cancellationToken);

    // Slice 3B: the code on a package label (C00042).
    Task<OperationsCustomer?> FindByCodeAsync(string code, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsCustomer>> ListByIdsAsync(IReadOnlyCollection<Guid> customerIds, CancellationToken cancellationToken);

    Task<OperationsCustomer?> FindByEmailAsync(EmailAddress email, CancellationToken cancellationToken);

    Task<OperationsCustomerSession?> FindSessionAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsCustomerSession>> ListOpenSessionsAsync(Guid customerId, CancellationToken cancellationToken);

    Task<OperationsCustomerToken?> FindTokenAsync(string tokenHash, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsCustomerToken>> ListOpenTokensAsync(Guid customerId, OperationsCustomerTokenPurpose purpose, CancellationToken cancellationToken);

    // Counts a failed sign-in in the database itself; true when the account is now locked.
    Task<bool> RecordFailedSignInAsync(Guid customerId, DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<(IReadOnlyList<OperationsCustomer> Items, int TotalCount)> ListAsync(string? search, int page, int pageSize, CancellationToken cancellationToken);

    // Slice 5C: the front-desk lookup: an exact email, an exact customer code, or a phone number ending with the digits typed (seven or more).
    Task<IReadOnlyList<OperationsCustomer>> FindForCounterAsync(string text, int take, CancellationToken cancellationToken);
}
