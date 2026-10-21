namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsDestinationOperator.cs
//
// PURPOSE:
// Who runs a destination warehouse: the company itself or a partner (Slice 4C).
//
// RESPONSIBILITIES:
// - Name the two operators
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsDestinationOperator
{
    Company = 1,

    Partner = 2
}
