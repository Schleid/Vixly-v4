using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsUserSession.cs
//
// PURPOSE:
// A signed-in staff session, kept on the server (Slice 1, owner decision D-O6).
//
// RESPONSIBILITIES:
// - Start with a hashed token
// - End after 15 idle minutes or 12 hours
// - Record the last second-factor time for step-up
//
// IMPORTANT NOTES:
// - Bound to its tenant by OperationsEntity
// - Last seen is written at most once a minute
// =========================================================
public sealed class OperationsUserSession : OperationsEntity
{
    private static readonly TimeSpan TouchInterval = TimeSpan.FromMinutes(1);

    private OperationsUserSession()
    {
        TokenHash = string.Empty;
    }

    private OperationsUserSession(Guid tenantId, Guid userId, string tokenHash, string? ipAddress, string? userAgent, bool setup, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        UserId = userId;
        TokenHash = tokenHash;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        IsSetupSession = setup;
        LastSeenUtc = createdUtc;
        SecondFactorUtc = createdUtc;
        ExpiresUtc = createdUtc.Add(OperationsIdentityLimits.SessionAbsoluteLifetime);
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    // A session started while the tenant is still being set up reaches only the setup pages (Slice 1, D-O3).
    public bool IsSetupSession { get; private set; }

    public DateTimeOffset LastSeenUtc { get; private set; }

    public DateTimeOffset SecondFactorUtc { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public DateTimeOffset? EndedUtc { get; private set; }

    public OperationsSessionEndReason? EndReason { get; private set; }

    public bool IsEnded => EndedUtc is not null;

    // =====================================================
    // METHOD: START
    // =====================================================
    public static OperationsUserSession Start(Guid tenantId, Guid userId, string tokenHash, string? ipAddress, string? userAgent, bool setup, DateTimeOffset utcNow)
    {
        if (userId == Guid.Empty)
        {
            throw new DomainException(OperationsIdentityErrors.UserRequired);
        }

        return new OperationsUserSession(
            tenantId,
            userId,
            OperationsTokenHash.Require(tokenHash),
            OperationsIdentityText.Clip(ipAddress, OperationsIdentityLimits.IpAddressMaxLength),
            OperationsIdentityText.Clip(userAgent, OperationsIdentityLimits.UserAgentMaxLength),
            setup,
            utcNow);
    }

    // =====================================================
    // METHOD: VALIDITY
    // =====================================================
    public OperationsSessionEndReason? ExpiryReasonAt(DateTimeOffset utcNow)
    {
        if (utcNow >= ExpiresUtc)
        {
            return OperationsSessionEndReason.AbsoluteLifetime;
        }

        if (utcNow - LastSeenUtc >= OperationsIdentityLimits.SessionIdleTimeout)
        {
            return OperationsSessionEndReason.IdleTimeout;
        }

        return null;
    }

    public bool IsValidAt(DateTimeOffset utcNow)
    {
        return !IsEnded && ExpiryReasonAt(utcNow) is null;
    }

    public bool Touch(DateTimeOffset utcNow)
    {
        if (IsEnded)
        {
            throw new DomainException(OperationsIdentityErrors.SessionEnded);
        }

        if (utcNow - LastSeenUtc < TouchInterval)
        {
            return false;
        }

        LastSeenUtc = utcNow;
        MarkAsModified(utcNow);
        return true;
    }

    // Step-up: the user just confirmed a second factor in this session (Slice 1D).
    public void ConfirmSecondFactor(DateTimeOffset utcNow)
    {
        if (IsEnded)
        {
            throw new DomainException(OperationsIdentityErrors.SessionEnded);
        }

        SecondFactorUtc = utcNow;
        MarkAsModified(utcNow);
    }

    // =====================================================
    // METHOD: END
    // =====================================================
    public void End(OperationsSessionEndReason reason, DateTimeOffset utcNow)
    {
        if (IsEnded)
        {
            return;
        }

        EndedUtc = utcNow;
        EndReason = reason;
        MarkAsModified(utcNow);
    }
}
