namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsLiabilityBasis.cs
//
// PURPOSE:
// How a company limits what it owes for a shipment the customer did not insure (Slice 5B, design section 4).
//
// RESPONSIBILITIES:
// - Name each basis the industry uses
//
// IMPORTANT NOTES:
// - Figures are the company's own: legal limits change and depend on the route (never hard-coded)
// =========================================================
public enum OperationsLiabilityBasis
{
    // Nothing beyond the insurance the company sells.
    None = 1,

    // An amount per kg or lb of the parcel lost or damaged, as the air and sea conventions do.
    PerWeight = 2,

    // An amount per parcel.
    PerParcel = 3,

    // The declared value, up to a cap.
    DeclaredValueUpToCap = 4
}
