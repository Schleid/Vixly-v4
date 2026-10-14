using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformPasskeys.cs
//
// PURPOSE:
// The WebAuthn adapter: builds passkey options for the browser
// and verifies what the browser sends back (ADR-0006).
//
// RESPONSIBILITIES:
// - Build registration options for a user, excluding the passkeys
//   they already have
// - Verify a registration answer against the options it answers
// - Build sign-in options: for any passkey (discoverable) or for
//   the given ones only
// - Read the credential id from a sign-in answer, then verify the
//   answer with the stored public key and counter
//
// IMPORTANT NOTES:
// - Options and answers are WebAuthn JSON; the Application layer
//   never looks inside
// - Verification failures answer null; the adapter logs why
// - User verification (PIN or biometric) is required every time,
//   so a passkey stands for both factors
// =========================================================
public interface IPlatformPasskeys
{
    string CreateRegistrationOptions(PlatformUser user, IReadOnlyCollection<byte[]> excludedCredentialIds);

    Task<PlatformPasskeyCredential?> VerifyRegistrationAsync(
        string optionsJson,
        string attestationJson,
        CancellationToken cancellationToken);

    string CreateAssertionOptions(IReadOnlyCollection<byte[]> allowedCredentialIds);

    byte[]? ReadCredentialId(string assertionJson);

    Task<long?> VerifyAssertionAsync(
        string optionsJson,
        string assertionJson,
        PlatformUserPasskey passkey,
        CancellationToken cancellationToken);
}
