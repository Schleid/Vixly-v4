namespace Vixly.Operations.Domain.Customers;

// =========================================================
// FILE: OperationsCustomerStatus.cs
//
// PURPOSE:
// Where a customer account stands (Slice 2).
//
// RESPONSIBILITIES:
// - Name each state
//
// IMPORTANT NOTES:
// - Stored as int; never renumber
// =========================================================
public enum OperationsCustomerStatus
{
    PendingVerification = 0,
    Active = 1,
    Suspended = 2,
    Closed = 3
}
