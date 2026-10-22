namespace Vixly.Operations.Domain.Customers;

// =========================================================
// FILE: OperationsCustomerTokenPurpose.cs
//
// PURPOSE:
// What a customer link is for (Slice 2).
//
// RESPONSIBILITIES:
// - Name each single-use link
//
// IMPORTANT NOTES:
// - Stored as int; never renumber
// =========================================================
public enum OperationsCustomerTokenPurpose
{
    Verification = 1,
    PasswordReset = 2
}
