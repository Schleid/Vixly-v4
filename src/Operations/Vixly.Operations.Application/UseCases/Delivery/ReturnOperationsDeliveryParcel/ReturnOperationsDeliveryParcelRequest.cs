namespace Vixly.Operations.Application.UseCases.Delivery.ReturnOperationsDeliveryParcel;

// =========================================================
// FILE: ReturnOperationsDeliveryParcelRequest.cs
//
// PURPOSE:
// A parcel that could not be delivered leaves for its origin: returned to the sender (Slice 7 step 3, R-14).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ReturnOperationsDeliveryParcelRequest
{
    public Guid WarehouseId { get; init; }

    public Guid ParcelId { get; init; }

    public string? Note { get; init; }
}
