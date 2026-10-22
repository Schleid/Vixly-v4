namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsPolicyScope.cs
//
// PURPOSE:
// What a company insurance policy protects (Slice 5B, I-6).
//
// RESPONSIBILITIES:
// - Tell the company's operations from its customers' shipments
//
// IMPORTANT NOTES:
// - The first choice on every policy; it decides which kinds fit
// =========================================================
public enum OperationsPolicyScope
{
    // Loss or damage the company causes while goods are in its hands: warehouses, hubs, vehicles, team.
    CompanyOperations = 1,

    // The cover customers buy for their shipments.
    CustomerShipments = 2
}
