using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformInvoiceHistory.cs
//
// PURPOSE:
// Reads an invoice's history.
//
// RESPONSIBILITIES:
// - List the history entries of one invoice
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure; append-only table
// =========================================================
public interface IPlatformInvoiceHistory
{
    Task<IReadOnlyList<PlatformInvoiceEvent>> ListAsync(Guid invoiceId, CancellationToken cancellationToken);
}
