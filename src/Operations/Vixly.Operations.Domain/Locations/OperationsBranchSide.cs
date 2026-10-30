namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsBranchSide.cs
//
// PURPOSE:
// Which side of the route a branch serves (sweep D-O10).
//
// RESPONSIBILITIES:
// - Name the sides
//
// IMPORTANT NOTES:
// - Stored as int
// =========================================================
public enum OperationsBranchSide
{
    Origin = 0,
    Destination = 1,
    Both = 2
}
