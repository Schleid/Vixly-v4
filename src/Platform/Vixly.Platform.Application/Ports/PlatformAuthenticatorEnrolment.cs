namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: PlatformAuthenticatorEnrolment.cs
//
// PURPOSE:
// What the console shows while a user adds an authenticator
// app.
//
// RESPONSIBILITIES:
// - Carry the encrypted secret to store
// - Carry the manual key and the otpauth URI for the QR code
//
// IMPORTANT NOTES:
// - The manual key and URI contain the plain secret: they
//   are shown once and never stored or logged
// =========================================================
public sealed record PlatformAuthenticatorEnrolment(
    string EncryptedSecret,
    string ManualKey,
    string OtpAuthUri);
