namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsUserAccountStatus.cs
//
// PURPOSE:
// Lifecycle of a staff account (Slice 1).
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as int; never renumber
// =========================================================
public enum OperationsUserAccountStatus
{
    Invited = 0,
    Active = 1,
    Suspended = 2,
    Locked = 3,
    Deactivated = 4
}
