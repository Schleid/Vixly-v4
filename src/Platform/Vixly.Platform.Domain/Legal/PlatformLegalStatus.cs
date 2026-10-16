namespace Vixly.Platform.Domain.Legal;

// =========================================================
// FILE: PlatformLegalStatus.cs
//
// PURPOSE:
// Where a tenant stands with the Terms.
//
// RESPONSIBILITIES:
// - Not asked, accepted, due before a deadline, or overdue
//
// IMPORTANT NOTES:
// - Overdue makes the tenant read-only for its staff (owner D-Lg6)
// =========================================================
public enum PlatformLegalStatus
{
    NotRequired = 0,
    Accepted = 1,
    Due = 2,
    Overdue = 3
}
