namespace Vixly.Operations.Domain.Supplies;

// =========================================================
// FILE: OperationsSupplyAdjustReason.cs
//
// PURPOSE:
// Why staff adjusted a supply's stock by hand (Slice 5E, S-4).
//
// RESPONSIBILITIES:
// - Name the reasons of an adjustment
//
// IMPORTANT NOTES:
// - Each adjustment also takes a note; text comes from OperationsText (ui.supplies.reason.*)
// =========================================================
public enum OperationsSupplyAdjustReason
{
    Damaged = 1,
    UsedByCompany = 2,
    Found = 3,
    Correction = 4,
    Other = 5
}
