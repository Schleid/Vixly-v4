namespace Vixly.Operations.Domain.Tracking;

// =========================================================
// FILE: OperationsHoldReasons.cs
//
// PURPOSE:
// Why a package is held (ADR-0018 controlled list).
//
// RESPONSIBILITIES:
// - Several reasons may be open at once
//
// IMPORTANT NOTES:
// - Stored as a bit set; never renumber
// =========================================================
[Flags]
public enum OperationsHoldReasons
{
    None = 0,
    Unidentified = 1,
    AwaitingCustomerInformation = 2,
    InspectionDocuments = 4,
    InspectionPermit = 8,
    InspectionValue = 16,
    InspectionFee = 32,
    Damaged = 64,
    Refused = 128,
    Other = 256,

    // Slice 4D: the customer's package arrived, but is not in one of its shipments yet.
    AwaitingShipment = 512
}
