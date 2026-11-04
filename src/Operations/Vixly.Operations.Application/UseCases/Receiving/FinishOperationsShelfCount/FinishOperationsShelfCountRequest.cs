namespace Vixly.Operations.Application.UseCases.Receiving.FinishOperationsShelfCount;

// =========================================================
// FILE: FinishOperationsShelfCountRequest.cs
//
// PURPOSE:
// Finishes a shelf count (Slice 12D, owner H-7): what was found, what is missing, what was recorded elsewhere, and unknown codes.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class FinishOperationsShelfCountRequest
{
    public Guid CountId { get; init; }
}
