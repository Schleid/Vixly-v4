namespace Vixly.Contracts.Operations.Packages;

// =========================================================
// FILE: CustomerParcelDto.cs
//
// PURPOSE:
// One of the customer's packages (Slice 3B).
//
// RESPONSIBILITIES:
// - Carry the values between Customer.Web and Operations.Api
//
// IMPORTANT NOTES:
// - Wire type: add fields at the end, never rename (ADR-0014)
// - Weight and sizes are in the company's units, named by the envelope
// =========================================================
public sealed record CustomerParcelDto(Guid ParcelId, string? Number, string? Carrier, string? TrackingNumber, string? Contents, decimal? DeclaredValue, string? CurrencyCode, decimal? Weight, decimal? Length, decimal? Width, decimal? Height, string? Milestone, IReadOnlyList<string> Holds, string? Warehouse, DateTimeOffset ReceivedUtc, Guid? ShipmentId = null);
