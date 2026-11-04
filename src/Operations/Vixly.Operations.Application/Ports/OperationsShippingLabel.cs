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
// - Owner, 2026-10-31: no customer number (the customer is often the sender, not the recipient) and no contents (a thief would read them)
// - Owner, 2026-10-31: the service's three-letter label code printed large, with a look per mode (air solid, ocean striped, ground outlined; express banded)
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
    string Weight,
    string? TrackingNumber = null,
    byte[]? Logo = null,
    string? LabelCode = null,
    string? ServiceName = null,
    string? ServiceMode = null,
    bool Express = false,
    string? ShipDate = null,
    string? OriginName = null,
    bool Insured = false,
    bool Consolidated = false);
