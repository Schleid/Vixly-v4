namespace Vixly.Platform.Application.UseCases.Billing.ResendPlatformInvoice;

// =========================================================
// FILE: ResendPlatformInvoiceRequest.cs
//
// PURPOSE:
// Emails an invoice again to the tenant's billing contact (D-B13).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ResendPlatformInvoiceRequest
{
    public Guid PlatformInvoiceId { get; init; }
}
