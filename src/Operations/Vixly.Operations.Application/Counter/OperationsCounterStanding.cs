using Vixly.Operations.Domain.Customers;

namespace Vixly.Operations.Application.Counter;

// =========================================================
// FILE: OperationsCounterStanding.cs
//
// PURPOSE:
// Where the customer at the counter stands, shown before anything else (owner, 2026-10-20: "it has to call for attention").
//
// RESPONSIBILITIES:
// - Say whether the customer is in good standing, and why not
// - Give the clerk the customer's figures at a glance: since when, shipments open and final, parcels on hold, last shipment
//
// IMPORTANT NOTES:
// - Not in good standing: no new shipment and no new parcel (owner, 2026-10-20)
// - Today the account's status decides; unpaid invoices past their date join with billing (Slice 5C step 2)
// - No credit, balances or notes here (C-9)
// =========================================================
public sealed record OperationsCounterStanding(
    bool Good,
    string? Reason,
    DateTimeOffset MemberSinceUtc,
    int OpenShipments,
    int FinalShipments,
    int ParcelsOnHold,
    DateTimeOffset? LastShipmentUtc)
{
    public static string? ReasonFor(OperationsCustomerStatus status) => status switch
    {
        OperationsCustomerStatus.Active => null,
        OperationsCustomerStatus.Suspended => "suspended",
        OperationsCustomerStatus.PendingVerification => "pending",
        _ => "closed"
    };
}
