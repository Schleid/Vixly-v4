namespace Vixly.Operations.Domain.Payables;

// =========================================================
// FILE: OperationsBillCategory.cs
//
// PURPOSE:
// What a bill is for (Slice 8), for the costs and for the accounting.
//
// RESPONSIBILITIES:
// - Name each category
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsBillCategory
{
    // Air, ocean or trucking: what a carrier charges for a dispatch.
    Freight = 1,

    // A partner agent's fee for its work at a destination.
    PartnerFee = 2,

    // A customs broker or the customs themselves.
    Customs = 3,

    // Local delivery, handling, storage paid to others.
    Handling = 4,

    // Boxes, tape and other supplies bought.
    Supplies = 5,

    Rent = 6,

    Utilities = 7,

    Other = 99
}
