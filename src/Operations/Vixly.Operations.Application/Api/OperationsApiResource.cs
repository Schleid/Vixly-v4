namespace Vixly.Operations.Application.Api;

// =========================================================
// FILE: OperationsApiResource.cs
//
// PURPOSE:
// What the company API can read (Slice 10).
//
// RESPONSIBILITIES:
// - Name each resource
//
// IMPORTANT NOTES:
// - Read only (owner, 2026-10-22)
// =========================================================
public enum OperationsApiResource
{
    Customers = 1,

    Shipments = 2,

    Parcels = 3,

    Tracking = 4,

    Invoices = 5,

    Payments = 6
}
