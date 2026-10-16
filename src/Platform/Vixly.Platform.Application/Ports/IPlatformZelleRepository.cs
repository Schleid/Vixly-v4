using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformZelleRepository.cs
//
// PURPOSE:
// Stores Vixly's Zelle details and the customers' Zelle reports.
//
// RESPONSIBILITIES:
// - Load and add the Zelle details and reports
// - Answer the questions the rules need: waiting report, reports per invoice
//
// IMPORTANT NOTES:
// - Saved by the unit of work
// =========================================================
public interface IPlatformZelleRepository
{
    Task<PlatformZelleAccount?> GetAccountAsync(CancellationToken cancellationToken);

    void AddAccount(PlatformZelleAccount account);

    Task<PlatformZelleClaim?> GetClaimAsync(Guid claimId, CancellationToken cancellationToken);

    Task<PlatformZelleClaim?> FindWaitingAsync(Guid invoiceId, CancellationToken cancellationToken);

    Task<int> CountForInvoiceAsync(Guid invoiceId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListWaitingInvoiceIdsAsync(Guid platformTenantId, CancellationToken cancellationToken);

    void AddClaim(PlatformZelleClaim claim);
}
