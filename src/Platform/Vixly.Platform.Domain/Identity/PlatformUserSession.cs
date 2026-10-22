using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformUserSession.cs
//
// PURPOSE:
// A server-side record of one signed-in staff session
// (ADR-0006 section 6).
//
// RESPONSIBILITIES:
// - Identify the session by the hash of its cookie token
// - Apply the idle timeout (5 minutes) and the absolute
//   lifetime (12 hours)
// - Record the last second-factor confirmation for step-up
// - End the session after 5 failed step-up codes in a row
// - End with a reason
//
// IMPORTANT NOTES:
// - Aggregate root, separate from PlatformUser because it is
//   written on almost every request
// - The raw token lives only in the browser cookie; the table
//   holds its SHA-256 hash, so a database leak gives no
//   usable session
// - Touch writes at most once a minute to keep writes cheap;
//   the idle check uses that time, so the effective idle
//   limit is between 5 and 6 minutes
// =========================================================
public sealed class PlatformUserSession : AuditableEntity
{
    private static readonly TimeSpan TouchInterval = TimeSpan.FromMinutes(1);

    private PlatformUserSession()
    {
        TokenHash = string.Empty;
    }

    private PlatformUserSession(
        Guid platformUserId,
        string tokenHash,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        PlatformUserId = platformUserId;
        TokenHash = tokenHash;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        LastSeenUtc = createdUtc;
        SecondFactorUtc = createdUtc;
        ExpiresUtc = createdUtc.Add(PlatformIdentityLimits.SessionAbsoluteLifetime);
    }

    public Guid PlatformUserId { get; private set; }

    public string TokenHash { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTimeOffset LastSeenUtc { get; private set; }

    public DateTimeOffset SecondFactorUtc { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public DateTimeOffset? EndedUtc { get; private set; }

    public PlatformSessionEndReason? EndReason { get; private set; }

    public int FailedStepUpCount { get; private set; }

    public bool IsEnded => EndedUtc is not null;

    // Owner, 2026-10-21: the country the sign-in came from (Cloudflare's two-letter code), for member activity.
    public string? Country { get; private set; }

    public void From(string? country)
    {
        string? code = country?.Trim().ToUpperInvariant();
        Country = code is { Length: 2 } && code.All(char.IsAsciiLetterOrDigit) ? code : null;
    }

    // =====================================================
    // METHOD: START
    // =====================================================
    public static PlatformUserSession Start(
        Guid platformUserId,
        string tokenHash,
        string? ipAddress,
        string? userAgent,
        DateTimeOffset utcNow)
    {
        if (platformUserId == Guid.Empty)
        {
            throw new DomainException(PlatformIdentityErrors.UserRequired);
        }

        return new PlatformUserSession(
            platformUserId,
            PlatformTokenHash.Require(tokenHash),
            Truncate(ipAddress, PlatformIdentityLimits.IpAddressMaxLength),
            Truncate(userAgent, PlatformIdentityLimits.UserAgentMaxLength),
            utcNow);
    }

    // =====================================================
    // METHOD: VALIDATE AND TOUCH
    // =====================================================
    public PlatformSessionEndReason? ExpiryReasonAt(DateTimeOffset utcNow)
    {
        if (utcNow >= ExpiresUtc)
        {
            return PlatformSessionEndReason.AbsoluteLifetime;
        }

        if (utcNow - LastSeenUtc >= PlatformIdentityLimits.SessionIdleTimeout + TouchInterval)
        {
            return PlatformSessionEndReason.IdleTimeout;
        }

        return null;
    }

    public bool IsValidAt(DateTimeOffset utcNow)
    {
        return !IsEnded && ExpiryReasonAt(utcNow) is null;
    }

    public bool Touch(DateTimeOffset utcNow)
    {
        EnsureOpen();

        if (utcNow - LastSeenUtc < TouchInterval)
        {
            return false;
        }

        LastSeenUtc = utcNow;
        MarkAsModified(utcNow);

        return true;
    }

    // =====================================================
    // METHOD: STEP-UP
    // =====================================================
    public void ConfirmSecondFactor(DateTimeOffset utcNow)
    {
        EnsureOpen();

        SecondFactorUtc = utcNow;
        FailedStepUpCount = 0;
        MarkAsModified(utcNow);
    }

    public bool RecordFailedStepUp(DateTimeOffset utcNow)
    {
        EnsureOpen();

        FailedStepUpCount++;
        MarkAsModified(utcNow);

        if (FailedStepUpCount < PlatformIdentityLimits.StepUpMaxFailures)
        {
            return false;
        }

        End(PlatformSessionEndReason.StepUpFailed, utcNow);

        return true;
    }

    public bool HasRecentSecondFactor(DateTimeOffset utcNow)
    {
        return !IsEnded && utcNow - SecondFactorUtc <= PlatformIdentityLimits.StepUpWindow;
    }

    // =====================================================
    // METHOD: END
    // =====================================================
    public void End(PlatformSessionEndReason reason, DateTimeOffset utcNow)
    {
        if (IsEnded)
        {
            return;
        }

        EndedUtc = utcNow;
        EndReason = reason;
        MarkAsModified(utcNow);
    }

    private void EnsureOpen()
    {
        if (IsEnded)
        {
            throw new DomainException(PlatformIdentityErrors.SessionEnded);
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        string trimmed = value.Trim();

        return trimmed.Length <= maxLength ? trimmed : trimmed[..maxLength];
    }
}
