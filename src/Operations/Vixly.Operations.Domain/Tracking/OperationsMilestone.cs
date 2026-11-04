namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsMilestone.cs
//
// PURPOSE:
// The milestones of a package and a shipment (ADR-0018).
//
// RESPONSIBILITIES:
// - Order the journey from pre-alert to delivery; three terminal ends
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// - Milestones come only from tracking events (ADR-0021)
// =========================================================
public enum OperationsMilestone
{
    PreAlerted = 0,
    ReceivedAtOrigin = 1,
    Booked = 2,
    AwaitingPayment = 3,
    Released = 4,
    Consolidated = 5,
    Manifested = 6,
    DepartedOrigin = 7,
    InTransit = 8,
    ArrivedDestinationCountry = 9,
    InCustoms = 10,
    CustomsCleared = 11,
    ArrivedDestinationWarehouse = 12,
    ReadyForPickup = 13,
    OutForDelivery = 14,
    Delivered = 15,
    Cancelled = 90,
    ReturnedToSender = 91,
    Disposed = 92
}
