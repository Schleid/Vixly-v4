using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformUserPasskey.cs
//
// PURPOSE:
// A WebAuthn passkey registered by a staff user (ADR-0006).
//
// RESPONSIBILITIES:
// - Hold the credential id, public key, signature counter,
//   authenticator model (AAGUID) and the user-given name
// - Reject a signature counter that goes backwards (a sign of
//   a cloned authenticator)
//
// IMPORTANT NOTES:
// - Part of the PlatformUser aggregate
// - Holds public material only; the private key never leaves
//   the user's device
// - Many passkeys (synced platform passkeys) always report a
//   counter of 0; the regression check applies only when both
//   values are above 0, as the WebAuthn specification allows
// =========================================================
public sealed class PlatformUserPasskey : AuditableEntity
{
    private PlatformUserPasskey()
    {
        CredentialId = Array.Empty<byte>();
        PublicKey = Array.Empty<byte>();
        Name = string.Empty;
    }

    internal PlatformUserPasskey(
        Guid platformUserId,
        byte[] credentialId,
        byte[] publicKey,
        long signatureCounter,
        Guid aaguid,
        string name,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformUserId = platformUserId;
        CredentialId = credentialId;
        PublicKey = publicKey;
        SignatureCounter = signatureCounter;
        Aaguid = aaguid;
        Name = name;
    }

    public Guid PlatformUserId { get; private set; }

    public byte[] CredentialId { get; private set; }

    public byte[] PublicKey { get; private set; }

    public long SignatureCounter { get; private set; }

    public Guid Aaguid { get; private set; }

    public string Name { get; private set; }

    public DateTimeOffset? LastUsedUtc { get; private set; }

    public bool Matches(byte[] credentialId)
    {
        return CredentialId.AsSpan().SequenceEqual(credentialId);
    }

    internal void RecordUse(long signatureCounter, DateTimeOffset utcNow)
    {
        if (signatureCounter < 0
            || (SignatureCounter > 0 && signatureCounter > 0 && signatureCounter <= SignatureCounter))
        {
            throw new DomainException(PlatformIdentityErrors.PasskeyCounterRegressed);
        }

        SignatureCounter = signatureCounter;
        LastUsedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
