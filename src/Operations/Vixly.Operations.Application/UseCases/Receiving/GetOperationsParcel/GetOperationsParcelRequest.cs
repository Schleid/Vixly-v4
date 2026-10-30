namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcel;

// =========================================================
// FILE: GetOperationsParcelRequest.cs
//
// PURPOSE:
// One parcel for staff: details, customer, shipment, history, photos and receipt (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsParcelRequest
{
    public Guid ParcelId { get; init; }
}
