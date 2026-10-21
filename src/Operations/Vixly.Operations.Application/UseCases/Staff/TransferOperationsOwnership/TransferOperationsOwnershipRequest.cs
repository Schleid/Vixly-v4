namespace Vixly.Operations.Application.UseCases.Staff.TransferOperationsOwnership;

// =========================================================
// FILE: TransferOperationsOwnershipRequest.cs
//
// PURPOSE:
// The Owner hands ownership to an active Administrator with a second factor (Slice 1D).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class TransferOperationsOwnershipRequest
{
    public Guid UserId { get; init; }
}
