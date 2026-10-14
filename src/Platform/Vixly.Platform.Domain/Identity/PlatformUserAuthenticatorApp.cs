using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformUserAuthenticatorApp.cs
//
// PURPOSE:
// A staff user's authenticator-app (TOTP, RFC 6238) second
// factor (ADR-0006).
//
// RESPONSIBILITIES:
// - Hold the encrypted shared secret
// - Record when enrolment was confirmed with a valid code
// - Refuse a code whose time step was already accepted, so a
//   seen code cannot be replayed
//
// IMPORTANT NOTES:
// - Part of the PlatformUser aggregate
// - The secret is encrypted by Infrastructure with the
//   Platform Data Protection key ring before it reaches the
//   domain; the domain never sees the plain secret
// - Code verification itself is an Infrastructure concern;
//   the domain receives the accepted time step
// =========================================================
public sealed class PlatformUserAuthenticatorApp : AuditableEntity
{
    private PlatformUserAuthenticatorApp()
    {
        EncryptedSecret = string.Empty;
    }

    internal PlatformUserAuthenticatorApp(Guid platformUserId, string encryptedSecret, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformUserId = platformUserId;
        EncryptedSecret = encryptedSecret;
    }

    public Guid PlatformUserId { get; private set; }

    public string EncryptedSecret { get; private set; }

    public DateTimeOffset? ConfirmedUtc { get; private set; }

    public long LastAcceptedTimeStep { get; private set; }

    public bool IsConfirmed => ConfirmedUtc is not null;

    internal void Confirm(long timeStep, DateTimeOffset utcNow)
    {
        if (IsConfirmed)
        {
            throw new DomainException(PlatformIdentityErrors.AuthenticatorNotPending);
        }

        LastAcceptedTimeStep = timeStep;
        ConfirmedUtc = utcNow;
        MarkAsModified(utcNow);
    }

    internal void RecordUse(long timeStep, DateTimeOffset utcNow)
    {
        if (!IsConfirmed)
        {
            throw new DomainException(PlatformIdentityErrors.AuthenticatorNotConfirmed);
        }

        if (timeStep <= LastAcceptedTimeStep)
        {
            throw new DomainException(PlatformIdentityErrors.AuthenticatorCodeReused);
        }

        LastAcceptedTimeStep = timeStep;
        MarkAsModified(utcNow);
    }
}
