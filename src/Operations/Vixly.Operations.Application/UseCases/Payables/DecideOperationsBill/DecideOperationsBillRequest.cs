namespace Vixly.Operations.Application.UseCases.Payables.DecideOperationsBill;

// =========================================================
// FILE: DecideOperationsBillRequest.cs
//
// PURPOSE:
// Approves a bill, or sends it back with a reason (Slice 8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class DecideOperationsBillRequest
{
    public Guid BillId { get; init; }

    public bool Approve { get; init; }

    public string? Reason { get; init; }

    // Review: the version of the bill the person read; a bill changed since is refused, never approved unseen.
    public string? Version { get; init; }
}
