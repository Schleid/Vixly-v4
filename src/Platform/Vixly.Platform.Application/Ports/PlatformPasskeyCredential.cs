namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: PlatformPasskeyCredential.cs
//
// PURPOSE:
// A new passkey the WebAuthn adapter has verified.
//
// RESPONSIBILITIES:
// - Carry what PlatformUser.RegisterPasskey needs
//
// IMPORTANT NOTES:
// - The public key is in COSE form, as WebAuthn returns it
// =========================================================
public sealed record PlatformPasskeyCredential(
    byte[] CredentialId,
    byte[] PublicKey,
    long SignatureCounter,
    Guid Aaguid);
