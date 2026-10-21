namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcelLabel;

// =========================================================
// FILE: GetOperationsParcelLabelRequest.cs
//
// PURPOSE:
// The ID barcode label of a parcel, as a PDF (Slice 3B, D-R3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsParcelLabelRequest
{
    public Guid ParcelId { get; init; }
}
