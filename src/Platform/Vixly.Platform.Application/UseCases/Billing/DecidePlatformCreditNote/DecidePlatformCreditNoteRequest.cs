namespace Vixly.Platform.Application.UseCases.Billing.DecidePlatformCreditNote;

// =========================================================
// FILE: DecidePlatformCreditNoteRequest.cs
//
// PURPOSE:
// The sales manager issues or rejects a requested credit note or void; never the requester (D-B6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class DecidePlatformCreditNoteRequest
{
    public Guid PlatformInvoiceId { get; init; }

    public Guid CreditId { get; init; }

    public bool Approve { get; init; }

    public string? Reason { get; init; }
}
