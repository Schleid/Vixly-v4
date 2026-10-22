namespace Vixly.Operations.Application.UseCases.Delivery.GetOperationsRunSheet;

// =========================================================
// FILE: GetOperationsRunSheetRequest.cs
//
// PURPOSE:
// A delivery route's run sheet as a PDF (Slice 7 step 3, R-7, R-12).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsRunSheetRequest
{
    public Guid RouteId { get; init; }
}
