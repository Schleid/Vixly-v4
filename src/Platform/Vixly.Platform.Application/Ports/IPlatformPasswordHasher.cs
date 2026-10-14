namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformPasswordHasher.cs
//
// PURPOSE:
// Hashes and checks staff passwords.
//
// RESPONSIBILITIES:
// - Produce a hash for a new password
// - Check a password and say whether the hash needs upgrading
//
// IMPORTANT NOTES:
// - BCrypt (decided); implemented in Infrastructure
// - Plain passwords are never stored or logged
// =========================================================
public interface IPlatformPasswordHasher
{
    string Hash(string password);

    PlatformPasswordCheck Verify(string password, string passwordHash);
}
