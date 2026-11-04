namespace Vixly.Operations.Application.UseCases.Holds.DecideOperationsUnclaimed;

// =========================================================
// FILE: DecideOperationsUnclaimedRequest.cs
//
// PURPOSE:
// The Owner or an administrator decides a package unclaimed past the company's days: return to sender or dispose of (Slice 3E, Q-R6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class DecideOperationsUnclaimedRequest
{
    public Guid ParcelId { get; init; }

    public string Decision { get; init; } = string.Empty;

    public string? Note { get; init; }

    public string CommandKey { get; init; } = string.Empty;
}
