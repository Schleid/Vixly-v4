namespace Vixly.Operations.Application.UseCases.Credit.DecideOperationsCreditCharge;

// =========================================================
// FILE: DecideOperationsCreditChargeRequest.cs
//
// PURPOSE:
// The credit department approves or refuses a charge above the limit (Slice 5F step 2b, K-3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class DecideOperationsCreditChargeRequest
{
    public Guid InvoiceId { get; init; }

    public bool Approve { get; init; }

    // Why: required to refuse, kept in the audit (review).
    public string? Note { get; init; }
}
