namespace Vixly.Operations.Application.UseCases.Inspection.DecideOperationsDisposal;

// =========================================================
// FILE: DecideOperationsDisposalRequest.cs
//
// PURPOSE:
// The Owner or an administrator approves or declines disposing of a refused parcel (Slice 3C, Q-C3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class DecideOperationsDisposalRequest
{
    public Guid InspectionId { get; init; }

    public bool Approve { get; init; }

    public string? Note { get; init; }

    public string CommandKey { get; init; } = string.Empty;
}
