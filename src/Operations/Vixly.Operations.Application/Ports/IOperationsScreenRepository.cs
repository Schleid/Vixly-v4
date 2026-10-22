using Vixly.Operations.Domain.Screens;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsScreenRepository.cs
//
// PURPOSE:
// The company's customer screens (Slice 5F step 3).
//
// RESPONSIBILITIES:
// - Add, remove, list and find screens, by id or by their secret's hash
// - Say whether the customer accepted the Zelle terms on a screen for an invoice
//
// IMPORTANT NOTES:
// - Tenant-filtered like every Operations read
// =========================================================
public interface IOperationsScreenRepository
{
    void Add(OperationsCustomerScreen screen);

    void Remove(OperationsCustomerScreen screen);

    Task<IReadOnlyList<OperationsCustomerScreen>> ListAsync(CancellationToken cancellationToken);

    Task<OperationsCustomerScreen?> GetAsync(Guid screenId, CancellationToken cancellationToken);

    Task<OperationsCustomerScreen?> FindBySecretAsync(string secretHash, CancellationToken cancellationToken);

    Task<OperationsCustomerScreen?> FindByCodeAsync(string pairingCodeHash, CancellationToken cancellationToken);

    void Add(OperationsScreenRecord record);

    // What customers accepted on screens for an invoice, newest first.
    Task<IReadOnlyList<OperationsScreenRecord>> ListRecordsAsync(Guid invoiceId, CancellationToken cancellationToken);

    // When the customer last accepted these Zelle terms on a screen for this invoice, since the time given.
    Task<DateTimeOffset?> ZelleAcceptedAsync(Guid invoiceId, string termsHash, DateTimeOffset since, CancellationToken cancellationToken);
}
