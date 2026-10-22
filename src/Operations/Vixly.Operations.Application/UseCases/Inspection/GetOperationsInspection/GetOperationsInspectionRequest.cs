namespace Vixly.Operations.Application.UseCases.Inspection.GetOperationsInspection;

// =========================================================
// FILE: GetOperationsInspectionRequest.cs
//
// PURPOSE:
// One inspection with its parcel, shipment and the restricted items that apply (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsInspectionRequest
{
    public Guid InspectionId { get; init; }
}
