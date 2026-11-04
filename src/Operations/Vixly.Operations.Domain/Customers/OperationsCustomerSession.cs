using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Identity;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Customers;

// =========================================================
// FILE: OperationsCustomerSession.cs
//
// PURPOSE:
// A customer's signed-in session, kept by Operations (Slice 2, D-C3).
//
// RESPONSIBILITIES:
// - Hold the token hash, idle and absolute limits; end with a reason
//
// IMPORTANT NOTES:
// - Customer.Web keeps only the token in its cookie; Operations checks it on every call (D-C4)
// =========================================================
public sealed class OperationsCustomerSession : OperationsEntity
{
    private static readonly TimeSpan TouchInterval = TimeSpan.FromMinutes(1);

    private OperationsCustomerSession()
    {
        TokenHash = string.Empty;
    }

    private OperationsCustomerSession(Guid tenantId, Guid customerId, string tokenHash, string? ipAddress, string? userAgent, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        CustomerId = customerId;
        TokenHash = tokenHash;
        IpAddress = ipAddress;
        UserAgent = userAgent;
        LastSeenUtc = createdUtc;
        ExpiresUtc = createdUtc.Add(OperationsCustomerLimits.SessionAbsoluteLifetime);
    }

    public Guid CustomerId { get; private set; }

    public string TokenHash { get; private set; }

    public string? IpAddress { get; private set; }

    public string? UserAgent { get; private set; }

    public DateTimeOffset LastSeenUtc { get; private set; }

    public DateTimeOffset ExpiresUtc { get; private set; }

    public DateTimeOffset? EndedUtc { get; private set; }

    public OperationsSessionEndReason? EndReason { get; private set; }

    public bool IsEnded => EndedUtc is not null;

    public static OperationsCustomerSession Start(Guid tenantId, Guid customerId, string tokenHash, string? ipAddress, string? userAgent, DateTimeOffset utcNow)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException(OperationsIdentityErrors.UserRequired);
        }

        return new OperationsCustomerSession(tenantId, customerId, OperationsTokenHash.Require(tokenHash),
            OperationsIdentityText.Clip(ipAddress, OperationsIdentityLimits.IpAddressMaxLength), OperationsIdentityText.Clip(userAgent, OperationsIdentityLimits.UserAgentMaxLength), utcNow);
    }

    public OperationsSessionEndReason? ExpiryReasonAt(DateTimeOffset utcNow) => ExpiryReasonAt(utcNow, OperationsCustomerLimits.SessionIdleTimeout);

    // The idle time is the company's (owner, 2026-10-27); the one above is the default.
    public OperationsSessionEndReason? ExpiryReasonAt(DateTimeOffset utcNow, TimeSpan idleTimeout) =>
        utcNow >= ExpiresUtc ? OperationsSessionEndReason.AbsoluteLifetime
        : utcNow - LastSeenUtc >= idleTimeout ? OperationsSessionEndReason.IdleTimeout
        : null;

    public bool IsValidAt(DateTimeOffset utcNow) => !IsEnded && ExpiryReasonAt(utcNow) is null;

    public bool Touch(DateTimeOffset utcNow)
    {
        if (IsEnded)
        {
            throw new DomainException(OperationsCustomerErrors.SessionEnded);
        }

        if (utcNow - LastSeenUtc < TouchInterval)
        {
            return false;
        }

        LastSeenUtc = utcNow;
        MarkAsModified(utcNow);
        return true;
    }

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
