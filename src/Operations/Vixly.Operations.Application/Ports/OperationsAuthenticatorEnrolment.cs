namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsAuthenticatorEnrolment.cs
//
// PURPOSE:
// A new authenticator app secret to show the user.
//
// RESPONSIBILITIES:
// - Carry the encrypted secret, the manual key and the otpauth URI
//
// IMPORTANT NOTES:
// - The manual key and URI are shown once, never stored
// =========================================================
public sealed record OperationsAuthenticatorEnrolment(string EncryptedSecret, string ManualKey, string OtpAuthUri);
