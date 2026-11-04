namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsAccountTokenPurpose.cs
//
// PURPOSE:
// What an emailed account link is for.
//
// RESPONSIBILITIES:
// - Name the purposes
//
// IMPORTANT NOTES:
// - Stored as int
// =========================================================
public enum OperationsAccountTokenPurpose
{
    Invitation = 0,
    PasswordReset = 1
}
