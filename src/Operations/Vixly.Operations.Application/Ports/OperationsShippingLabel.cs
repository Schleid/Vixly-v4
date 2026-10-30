namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsShippingLabel.cs
//
// PURPOSE:
// What the 4 x 6 inch shipping label of a parcel shows (Slice 4D step 2, design section 5).
//
// RESPONSIBILITIES:
// - The shipment number, the parcel and its place in the shipment, the route, the service, the recipient's name and city, the delivery, the weight
//
// IMPORTANT NOTES:
// - No price and no contact of the customer or the recipient beyond the recipient's name and city
// =========================================================
public sealed record OperationsShippingLabel(
    string CompanyName,
    string ShipmentNumber,
    string ParcelNumber,
    int Piece,
    int Pieces,
    string OriginCode,
    string DestinationCode,
    string? ServiceCode,
    string RecipientName,
    string RecipientCity,
    bool Pickup,
    string Weight);
