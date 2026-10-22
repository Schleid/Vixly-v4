namespace Vixly.Operations.Application.UseCases.Holds.CloseOperationsStockCount;

// =========================================================
// FILE: CloseOperationsStockCountRequest.cs
//
// PURPOSE:
// Closes a count once every parcel is marked; a parcel missing needs a note and the Owner or an administrator (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CloseOperationsStockCountRequest
{
    public Guid CountId { get; init; }
}
