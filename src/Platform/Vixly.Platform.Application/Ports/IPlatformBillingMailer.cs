using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformBillingMailer.cs
//
// PURPOSE:
// Sends invoices, credit notes and receipts to the tenant's billing contact (D-B13).
//
// RESPONSIBILITIES:
// - Email one stored document as an attachment, in the document's language
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure
// - Called from the outbox, never inside a request
// =========================================================
public interface IPlatformBillingMailer
{
    Task SendDocumentAsync(
        PlatformTenant tenant,
        PlatformTenantContact recipient,
        PlatformInvoice invoice,
        PlatformBillingDocument document,
        string? payLink,
        CancellationToken cancellationToken);
}
