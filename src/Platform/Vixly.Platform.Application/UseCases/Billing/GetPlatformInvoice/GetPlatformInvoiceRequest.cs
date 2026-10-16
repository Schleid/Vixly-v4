namespace Vixly.Platform.Application.UseCases.Billing.GetPlatformInvoice;

// =========================================================
// FILE: GetPlatformInvoiceRequest.cs
//
// PURPOSE:
// Reads one invoice with its lines, payments, credit notes and history.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformInvoiceRequest
{
    public Guid PlatformInvoiceId { get; init; }
}
