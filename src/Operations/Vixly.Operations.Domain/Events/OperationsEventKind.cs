namespace Vixly.Operations.Domain.Events;

// =========================================================
// FILE: OperationsEventKind.cs
//
// PURPOSE:
// What a person can record by hand (Slice 6C, E-1).
//
// RESPONSIBILITIES:
// - Name the kinds
//
// IMPORTANT NOTES:
// - Milestones come only from the workflows; holds stay in the holds workflow
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsEventKind
{
    // Internal: staff only.
    Note = 1,

    // The customer sees the reason and an optional line.
    Delay = 2,

    Exception = 3
}
