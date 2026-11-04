namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsParcelLabel.cs
//
// PURPOSE:
// What the parcel ID label shows (Slice 3B, D-R3).
//
// RESPONSIBILITIES:
// - The parcel number (barcode and text), the customer code, the warehouse and the date
//
// IMPORTANT NOTES:
// - The shipping label (service, destination, tracking number) comes after inspection, in Slice 5
// =========================================================
public sealed record OperationsParcelLabel(string ParcelNumber, string? CustomerCode, string CompanyName, string WarehouseCode, DateTimeOffset ReceivedUtc);
