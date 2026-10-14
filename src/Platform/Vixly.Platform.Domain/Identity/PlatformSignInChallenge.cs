using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformSignInChallenge.cs
//
// PURPOSE:
// The short-lived state between the password step and the
// second-factor step of a staff sign-in (ADR-0006).
//
// RESPONSIBILITIES:
// - Bind the pending sign-in to one user by a token hash
// - Expire after 5 minutes
// - Allow at most 5 wrong second-factor attempts
// - Be completed once
//
// IMPORTANT NOTES:
// - Aggregate root
// - Stage Enrolment is used when the user has no second
//   factor yet (first sign-in, or after an administrator
//   reset): the only way forward is to enrol one
// - The raw token travels in a short-lived, HttpOnly cookie;
//   the table holds its hash
// =========================================================
public sealed class PlatformSignInChallenge : AuditableEntity
{
    public const int MaxFailedAttempts = 5;

    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private PlatformSignInChallenge()
    {
        TokenHash = string.Empty;
    }

    private PlatformSignInChallenge(
        Guid platformUserId,
        string tokenHash,
        PlatformSignInStage stage,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformUserId = platformUserId;
        TokenHash = tokenHash;
        Stage = stage;
        ExpiresUtc = createdUtc.Add(Lifetime);
    }

    public Guid PlatformUserId { get; private set; }

    public string TokenHash { get; private set; }

    public PlatformSignInStage Stage { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public int FailedAttempts { get; private set; }

    public DateTimeOffset? CompletedUtc { get; private set; }

    // =====================================================
    // METHOD: ISSUE
    // =====================================================
    public static PlatformSignInChallenge Issue(
        Guid platformUserId,
        string tokenHash,
        PlatformSignInStage stage,
        DateTimeOffset utcNow)
    {
        if (platformUserId == Guid.Empty)
        {
            throw new DomainException(PlatformIdentityErrors.UserRequired);
        }

        return new PlatformSignInChallenge(platformUserId, PlatformTokenHash.Require(tokenHash), stage, utcNow);
    }

    // =====================================================
    // METHOD: STATE
    // =====================================================
    public bool IsUsableAt(DateTimeOffset utcNow)
    {
        return CompletedUtc is null && FailedAttempts < MaxFailedAttempts && utcNow < ExpiresUtc;
    }

    public void EnsureUsable(PlatformSignInStage stage, DateTimeOffset utcNow)
    {
        if (!IsUsableAt(utcNow) || Stage != stage)
        {
            throw new DomainException(PlatformIdentityErrors.SignInChallengeInvalid);
        }
    }

    public void RecordFailedAttempt(DateTimeOffset utcNow)
    {
        FailedAttempts++;
        MarkAsModified(utcNow);
    }

    public void Complete(DateTimeOffset utcNow)
    {
        CompletedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
