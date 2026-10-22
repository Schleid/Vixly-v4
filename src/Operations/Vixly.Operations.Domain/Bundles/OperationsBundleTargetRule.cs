namespace Vixly.Operations.Domain.Bundles;

// =========================================================
// FILE: OperationsBundleTargetRule.cs
//
// PURPOSE:
// What happens when a bundle reaches its target (Slice 6A, D-1 and D-5; owner, A-2).
//
// RESPONSIBILITIES:
// - Name the company's choices
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsBundleTargetRule
{
    Warn = 1,
    Refuse = 2
}
