namespace Vixly.Platform.Application.UseCases.Billing.RequestPlatformCreditNote;

// =========================================================
// FILE: RequestPlatformCreditNoteRequest.cs
//
// PURPOSE:
// Asks the sales manager to approve a credit note or a void (D-B6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RequestPlatformCreditNoteRequest
{
    public Guid PlatformInvoiceId { get; init; }

    public decimal Amount { get; init; }

    public bool Void { get; init; }

    public string? Reason { get; init; }
}
