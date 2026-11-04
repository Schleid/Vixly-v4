using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Dispatch;

// =========================================================
// FILE: OperationsShipperLetter.cs
//
// PURPOSE:
// What the Shipper's Letter of Instruction shows (Slice 6B step 2, D-6; as V3).
//
// RESPONSIBILITIES:
// - Carry the shipper (the company), the consignee, the carrier and account, the airports, the master air waybill, pieces, weights, goods, value, charges, export reference, documents and instructions
//
// IMPORTANT NOTES:
// - One for the whole dispatch (all its bundles)
// =========================================================
public sealed record OperationsShipperLetter(string CompanyName, OperationsWaybillParty Shipper, OperationsWaybillParty Consignee, string? Carrier, string? AccountNumber, string Departure,
    string Destination, string? MasterWaybill, string DispatchNumber, int Pieces, decimal WeightKg, decimal ChargeableKg, IReadOnlyDictionary<string, decimal> DeclaredValues, bool Collect,
    string? ExportReference, OperationsDispatchDocuments Documents, string? Handling, DateOnly IssuedOn, OperationsWeightUnit WeightUnit);
