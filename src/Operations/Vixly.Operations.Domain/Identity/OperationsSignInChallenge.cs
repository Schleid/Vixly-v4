using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsSignInChallenge.cs
//
// PURPOSE:
// A sign-in between the password and the second factor (Slice 1).
//
// RESPONSIBILITIES:
// - Live 5 minutes, allow 5 wrong codes, complete once
//
// IMPORTANT NOTES:
// - Its token is in a short-lived cookie; only the hash is stored
// =========================================================
public sealed class OperationsSignInChallenge : OperationsEntity
{
    public const int MaxFailedAttempts = 5;

    public static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    private OperationsSignInChallenge()
    {
        TokenHash = string.Empty;
    }

    private OperationsSignInChallenge(Guid tenantId, Guid userId, string tokenHash, OperationsSignInStage stage, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        UserId = userId;
        TokenHash = tokenHash;
        Stage = stage;
        ExpiresUtc = createdUtc.Add(Lifetime);
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public OperationsSignInStage Stage { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public int FailedAttempts { get; private set; }

    public DateTimeOffset? CompletedUtc { get; private set; }

    public static OperationsSignInChallenge Issue(Guid tenantId, Guid userId, string tokenHash, OperationsSignInStage stage, DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(OperationsIdentityErrors.UserRequired);
        }

        return new OperationsSignInChallenge(tenantId, userId, OperationsTokenHash.Require(tokenHash), stage, utcNow);
    }

    public bool IsUsableAt(DateTimeOffset utcNow)
    {
        return CompletedUtc is null && FailedAttempts < MaxFailedAttempts && utcNow < ExpiresUtc;
    }

    public void RecordFailedAttempt(DateTimeOffset utcNow)
    {
        FailedAttempts++;
        MarkAsModified(utcNow);
    }

    public void Complete(DateTimeOffset utcNow)
    {
        if (!IsUsableAt(utcNow))
        {
            throw new DomainException(OperationsIdentityErrors.SignInChallengeInvalid);
        }

        CompletedUtc = utcNow;
        MarkAsModified(utcNow);
    }
}
