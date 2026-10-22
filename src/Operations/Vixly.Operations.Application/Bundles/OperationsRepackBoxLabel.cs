namespace Vixly.Operations.Application.Bundles;

// =========================================================
// FILE: OperationsRepackBoxLabel.cs
//
// PURPOSE:
// What a repack box's 4 x 6 label shows (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Carry its number, its shipment, its route and its weight
//
// IMPORTANT NOTES:
// - Weight in the company's units, already written
// =========================================================
public sealed record OperationsRepackBoxLabel(string CompanyName, string Number, string? ShipmentNumber, string OriginCode, string DestinationCode, string? ServiceCode, string RecipientName,
    int Parcels, string? Weight);
