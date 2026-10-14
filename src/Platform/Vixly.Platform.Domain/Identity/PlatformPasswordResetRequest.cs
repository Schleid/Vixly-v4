using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformPasswordResetRequest.cs
//
// PURPOSE:
// A record of a "forgot password" request, used to throttle
// requests per email and per client (V3 behaviour, kept).
//
// RESPONSIBILITIES:
// - Hold the email asked for, the client IP, the time and
//   whether a link was actually sent
//
// IMPORTANT NOTES:
// - Aggregate root
// - Written for every request, including unknown emails, so
//   the throttle cannot be used to discover which emails
//   exist. The response to the caller is always the same
// - Rows older than 30 days are removed by a Worker job
// =========================================================
public sealed class PlatformPasswordResetRequest : AuditableEntity
{
    private PlatformPasswordResetRequest()
    {
        Email = null!;
    }

    private PlatformPasswordResetRequest(
        EmailAddress email,
        string? ipAddress,
        bool linkSent,
        DateTimeOffset createdUtc)
        : base(createdUtc)
    {
        Email = email;
        IpAddress = ipAddress;
        LinkSent = linkSent;
    }

    public EmailAddress Email { get; private set; }

    public string? IpAddress { get; private set; }

    public bool LinkSent { get; private set; }

    public static PlatformPasswordResetRequest Record(
        EmailAddress email,
        string? ipAddress,
        bool linkSent,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(email);

        string? ip = string.IsNullOrWhiteSpace(ipAddress) ? null : ipAddress.Trim();

        if (ip is not null && ip.Length > PlatformIdentityLimits.IpAddressMaxLength)
        {
            ip = ip[..PlatformIdentityLimits.IpAddressMaxLength];
        }

        return new PlatformPasswordResetRequest(email, ip, linkSent, utcNow);
    }
}
