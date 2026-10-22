namespace Vixly.Operations.Application.UseCases.Holds.GetOperationsStockCount;

// =========================================================
// FILE: GetOperationsStockCountRequest.cs
//
// PURPOSE:
// One count of the undeclared parcels area, with what each parcel was found to be (Slice 4D step 3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsStockCountRequest
{
    public Guid CountId { get; init; }
}
