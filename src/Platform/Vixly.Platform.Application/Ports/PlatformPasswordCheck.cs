namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: PlatformPasswordCheck.cs
//
// PURPOSE:
// The outcome of checking a password against a stored hash.
//
// RESPONSIBILITIES:
// - Say whether the password matched
// - Say whether the hash should be upgraded
//
// IMPORTANT NOTES:
// - SucceededNeedsRehash covers migrated V3 hashes made at a
//   lower BCrypt cost (ADR-0006 section 4)
// =========================================================
public enum PlatformPasswordCheck
{
    Failed = 0,
    Succeeded = 1,
    SucceededNeedsRehash = 2
}
