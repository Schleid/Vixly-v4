namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformAccountTokenPurpose.cs
//
// PURPOSE:
// What a one-time account link is for.
//
// RESPONSIBILITIES:
// - Separate invitation links from password-reset links
//
// IMPORTANT NOTES:
// - Persisted as numbers; never renumber
// - V3 kept the two in separate tables; V4 uses one table
// =========================================================
public enum PlatformAccountTokenPurpose
{
    Invitation = 0,
    PasswordReset = 1
}
