using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Dispatch;

// =========================================================
// FILE: OperationsHouseWaybill.cs
//
// PURPOSE:
// What one house air waybill shows (Slice 6B step 2, D-6; owner, A-4 and A-5).
//
// RESPONSIBILITIES:
// - Carry the issuing agent, the master and house numbers, shipper and consignee, the route, pieces, weights, goods, value and charges
//
// IMPORTANT NOTES:
// - One per customer shipment on the dispatch; printed by the company under the airline's master air waybill
// =========================================================
public sealed record OperationsHouseWaybill(string CompanyName, OperationsWaybillParty Agent, string? MasterWaybill, string HouseNumber, OperationsWaybillParty Shipper, OperationsWaybillParty Consignee,
    string Departure, string Destination, IReadOnlyList<string> Routing, int Pieces, decimal WeightKg, decimal ChargeableKg, string Contents, decimal DeclaredValue, string CurrencyCode, bool Collect,
    string? Handling, DateOnly IssuedOn, string IssuedAt, OperationsWeightUnit WeightUnit);
