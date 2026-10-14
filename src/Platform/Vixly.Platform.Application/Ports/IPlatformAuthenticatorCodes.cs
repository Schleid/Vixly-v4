namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformAuthenticatorCodes.cs
//
// PURPOSE:
// Creates authenticator-app secrets and checks their codes
// (TOTP, RFC 6238).
//
// RESPONSIBILITIES:
// - Create a new encrypted secret for enrolment
// - Reopen a pending secret, so the enrolment page shows the
//   same QR code again after a refresh or a wrong code
// - Check a 6-digit code and return the matched time step
//
// IMPORTANT NOTES:
// - Returns null for a wrong code. The domain then refuses a
//   time step that was already used (no replay)
// - One step of clock drift either way is accepted
// =========================================================
public interface IPlatformAuthenticatorCodes
{
    PlatformAuthenticatorEnrolment CreateEnrolment(string accountLabel);

    PlatformAuthenticatorEnrolment? Reopen(string encryptedSecret, string accountLabel);

    long? Verify(string encryptedSecret, string code, DateTimeOffset utcNow);
}
