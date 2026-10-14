using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformUserRecoveryCode.cs
//
// PURPOSE:
// One single-use recovery code of a staff user (ADR-0006).
//
// RESPONSIBILITIES:
// - Hold the code's hash and when it was used
//
// IMPORTANT NOTES:
// - Part of the PlatformUser aggregate
// - Only the SHA-256 hash is stored. Codes are shown once at
//   generation and are random enough that a fast hash is safe
// =========================================================
public sealed class PlatformUserRecoveryCode : AuditableEntity
{
    private PlatformUserRecoveryCode()
    {
        CodeHash = string.Empty;
    }

    internal PlatformUserRecoveryCode(Guid platformUserId, string codeHash, DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformUserId = platformUserId;
        CodeHash = codeHash;
    }

    public Guid PlatformUserId { get; private set; }

    public string CodeHash { get; private set; }

    public DateTimeOffset? UsedUtc { get; private set; }

    public bool IsUsed => UsedUtc is not null;

    internal void MarkUsed(DateTimeOffset utcNow)
    {
        UsedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
