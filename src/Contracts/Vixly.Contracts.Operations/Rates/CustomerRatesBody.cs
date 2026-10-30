namespace Vixly.Contracts.Operations.Rates;

// =========================================================
// FILE: CustomerRatesBody.cs
//
// PURPOSE:
// The customer's calculator question, or the shipment to estimate (Slice 4).
//
// RESPONSIBILITIES:
// - Carry the destination, pieces and options, or a shipment
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// - A shipment id: the estimate of that shipment's measured packages
// =========================================================
public sealed record CustomerRatesBody(string? Country, IReadOnlyList<CustomerPieceBody>? Pieces, bool Consolidated, bool Insured, Guid? ShipmentId, Guid? Destination = null, bool HomeDelivery = false, Guid? Origin = null);
