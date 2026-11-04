namespace Vixly.Operations.Domain.Charges;

// =========================================================
// FILE: OperationsCustomsPolicy.cs
//
// PURPOSE:
// Who clears customs on a price list: the company, with the duties in its price, or the recipient (Slice 4C, owner 2026-10-19).
//
// RESPONSIBILITIES:
// - Name the two policies
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// - Transport only: the recipient shows proof of clearance before release
// =========================================================
public enum OperationsCustomsPolicy
{
    // The price includes duties; the company clears customs at destination.
    DutiesIncluded = 1,

    // The price is transport only; the recipient clears customs and pays the duties.
    TransportOnly = 2
}
