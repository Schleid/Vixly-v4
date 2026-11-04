namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerShipmentEstimateBody.cs
//
// PURPOSE:
// What the portal sends for a shipment estimate while the customer builds a shipment (Slice 5D).
//
// RESPONSIBILITIES:
// - Carry the values between Operations and the customer portal
//
// IMPORTANT NOTES:
// - Codes, numbers and the customer's own names only: the portal writes the words (ADR-0014)
// =========================================================
public sealed record CustomerShipmentEstimateBody(Vixly.Contracts.Operations.Packages.CustomerShipmentInstructionsBody? Instructions, Guid? OriginId, decimal? ExpectedValue, Guid? ShipmentId);
