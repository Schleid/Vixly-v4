namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsLocationStatus.cs
//
// PURPOSE:
// Whether a branch or warehouse is in use.
//
// RESPONSIBILITIES:
// - Name the states
//
// IMPORTANT NOTES:
// - Stored as int
// =========================================================
public enum OperationsLocationStatus
{
    Active = 0,
    Inactive = 1
}
