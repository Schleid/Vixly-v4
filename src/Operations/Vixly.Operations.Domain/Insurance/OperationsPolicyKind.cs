namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsPolicyKind.cs
//
// PURPOSE:
// The industry kinds of insurance policy a company holds (Slice 5B, design section 3).
//
// RESPONSIBILITIES:
// - Name each kind
//
// IMPORTANT NOTES:
// - Which kinds fit which scope: OperationsInsurancePolicy.Fits
// =========================================================
public enum OperationsPolicyKind
{
    FreightForwarderLiability = 1,

    WarehouseLegalLiability = 2,

    // The company's own trucks.
    CarrierLiability = 3,

    GeneralLiability = 4,

    // Cargo, shipments declared to the insurer each period.
    OpenCargo = 5,

    // Cargo, one policy per shipment.
    ShipmentCargo = 6,

    // No insurer: the company pays from its own funds within its own limits.
    SelfInsured = 7,

    Other = 8
}
