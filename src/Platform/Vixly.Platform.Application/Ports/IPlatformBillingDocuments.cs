using Vixly.Platform.Domain.Billing;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformBillingDocuments.cs
//
// PURPOSE:
// Renders invoice, credit note and receipt PDFs (D-B10, D-B11).
//
// RESPONSIBILITIES:
// - Turn a billing record into PDF bytes in the document's language
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure (QuestPDF)
// - Called once at issue; the bytes are stored, never re-rendered
// =========================================================
public interface IPlatformBillingDocuments
{
    byte[] RenderInvoice(PlatformInvoice invoice, PlatformCompanyProfile issuer, PlatformBillingSettings settings);

    byte[] RenderCreditNote(PlatformInvoice invoice, PlatformCreditNote credit, PlatformCompanyProfile issuer, PlatformBillingSettings settings);

    byte[] RenderReceipt(PlatformInvoice invoice, PlatformPayment payment, PlatformCompanyProfile issuer, PlatformBillingSettings settings);
}
