namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsCarrierKind.cs
//
// PURPOSE:
// How a carrier moves goods, and how a dispatch leg travels (Slice 6B, D-1 and D-4).
//
// RESPONSIBILITIES:
// - Name the kinds
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsCarrierKind
{
    Air = 1,
    Ocean = 2,
    Ground = 3
}
