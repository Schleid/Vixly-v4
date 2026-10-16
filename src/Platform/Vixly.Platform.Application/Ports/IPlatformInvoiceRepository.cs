using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformInvoiceRepository.cs
//
// PURPOSE:
// Loads and stores invoices with their lines, payments, credit notes and history (Slice 4).
//
// RESPONSIBILITIES:
// - Find an invoice by id or by the event it bills
// - List what the past-due clock must look at
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure; the use case commits
// =========================================================
public interface IPlatformInvoiceRepository
{
    Task<PlatformInvoice?> GetAsync(Guid invoiceId, CancellationToken cancellationToken);

    Task<bool> SourceBilledAsync(string sourceKey, CancellationToken cancellationToken);

    // Slice 5: the newest open first-period invoice of a tenant (the registration invoice).
    Task<Guid?> FindOpenFirstPeriodAsync(Guid platformTenantId, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListOverdueUnmarkedAsync(DateTimeOffset utcNow, int take, CancellationToken cancellationToken);

    // Collections (Slice 6): the tenant's Issued invoices with a balance after their due date, credits loaded.
    Task<IReadOnlyList<PlatformInvoice>> ListOverdueForTenantAsync(Guid platformTenantId, DateTimeOffset utcNow, CancellationToken cancellationToken);

    Task<bool> HasOverdueAsync(Guid platformSubscriptionId, Guid exceptInvoiceId, DateTimeOffset utcNow, CancellationToken cancellationToken);

    void Add(PlatformInvoice invoice);

    void AddEvents(IEnumerable<PlatformInvoiceEvent> events);
}
