namespace Vixly.Operations.Application.UseCases.Printing.GetOperationsShippingLabel;

// =========================================================
// FILE: GetOperationsShippingLabelRequest.cs
//
// PURPOSE:
// The shipping label of a cleared parcel as a PDF, for a station that prints from the browser (Slice 4D step 2).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsShippingLabelRequest
{
    public Guid ParcelId { get; init; }
}
