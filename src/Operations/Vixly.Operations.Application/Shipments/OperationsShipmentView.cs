namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsShipmentView.cs
//
// PURPOSE:
// The views of the shipments desk (Slice 5A).
//
// RESPONSIBILITIES:
// - Name each view
//
// IMPORTANT NOTES:
// - All means every status
// =========================================================
public enum OperationsShipmentView
{
    All = 0,

    // Open, with where it goes: the customer's shipments to send.
    ToSend = 1,

    // The waiting shipments of Slice 4D: packages not yet placed by their customer.
    Waiting = 2,

    Cancelled = 3
}
