using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Inspection;

namespace Vixly.Operations.Application.Inspection;

// =========================================================
// FILE: OperationsHeldParcel.cs
//
// PURPOSE:
// One parcel of the holds page, with its latest inspection (Slice 3C design section 6).
//
// RESPONSIBILITIES:
// - Carry the parcel and what its inspection asked of the customer
//
// IMPORTANT NOTES:
// - Inspection is null for holds opened at the dock or the station
// =========================================================
public sealed record OperationsHeldParcel(OperationsParcelSummary Parcel, OperationsInspection? Inspection)
{
    // The customer's latest answer to each hold (Slice 3E).
    public IReadOnlyList<Vixly.Operations.Domain.Holds.OperationsHoldAnswer> Answers { get; init; } = Array.Empty<Vixly.Operations.Domain.Holds.OperationsHoldAnswer>();
}
