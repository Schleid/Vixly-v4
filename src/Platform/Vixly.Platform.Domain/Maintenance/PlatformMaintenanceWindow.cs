using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Maintenance;

// =========================================================
// FILE: PlatformMaintenanceWindow.cs
//
// PURPOSE:
// A maintenance window Vixly IT publishes in Control (owner, Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Hold the start, the end, the sites closed, the advance notice and the message in en, fr and es
// - Hold the hash of the pass link that lets IT through during the window
// - Cancel before the start; end early while running
//
// IMPORTANT NOTES:
// - Starts within a year and lasts at most 7 days
// - The advance notice is 0 to 168 hours before the start
// - A message is optional (500 characters); the sites add their own wording around it
// - Times are UTC
// =========================================================
public sealed class PlatformMaintenanceWindow : AuditableEntity
{
    public const int MessageMaxLength = 500;

    public const int PassHashMaxLength = 128;

    public const int MaxNoticeHours = 168;

    public static readonly TimeSpan MaxLength = TimeSpan.FromDays(7);

    public static readonly TimeSpan MaxLead = TimeSpan.FromDays(366);

    // A start a few minutes in the past is the person pressing Save after the minute turned.
    public static readonly TimeSpan StartGrace = TimeSpan.FromMinutes(5);

    private PlatformMaintenanceWindow()
    {
        PassHash = string.Empty;
    }

    private PlatformMaintenanceWindow(DateTimeOffset utcNow)
        : base(utcNow)
    {
        PassHash = string.Empty;
    }

    public DateTimeOffset StartsUtc { get; private set; }

    public DateTimeOffset EndsUtc { get; private set; }

    public PlatformMaintenanceSites Sites { get; private set; }

    public int NoticeHours { get; private set; }

    public string? MessageEn { get; private set; }

    public string? MessageFr { get; private set; }

    public string? MessageEs { get; private set; }

    public string PassHash { get; private set; }

    public Guid? CreatedByUserId { get; private set; }

    public DateTimeOffset? CancelledUtc { get; private set; }

    public DateTimeOffset? EndedEarlyUtc { get; private set; }

    public Guid? StoppedByUserId { get; private set; }

    public DateTimeOffset AnnouncedUtc => StartsUtc.AddHours(-NoticeHours);

    // =====================================================
    // METHOD: SCHEDULE
    // =====================================================
    public static PlatformMaintenanceWindow Schedule(
        DateTimeOffset startsUtc,
        DateTimeOffset endsUtc,
        PlatformMaintenanceSites sites,
        int noticeHours,
        string? messageEn,
        string? messageFr,
        string? messageEs,
        string passHash,
        Guid? createdByUserId,
        DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passHash);

        if (endsUtc <= startsUtc || startsUtc < utcNow - StartGrace || startsUtc > utcNow + MaxLead)
        {
            throw new DomainException(PlatformMaintenanceErrors.TimesInvalid);
        }

        if (endsUtc - startsUtc > MaxLength)
        {
            throw new DomainException(PlatformMaintenanceErrors.TooLong);
        }

        if ((sites & PlatformMaintenanceSites.All) == PlatformMaintenanceSites.None || (sites & ~PlatformMaintenanceSites.All) != PlatformMaintenanceSites.None)
        {
            throw new DomainException(PlatformMaintenanceErrors.SitesRequired);
        }

        if (noticeHours is < 0 or > MaxNoticeHours)
        {
            throw new DomainException(PlatformMaintenanceErrors.NoticeInvalid);
        }

        if (passHash.Length > PassHashMaxLength)
        {
            throw new ArgumentException("The pass hash is too long.", nameof(passHash));
        }

        return new PlatformMaintenanceWindow(utcNow)
        {
            StartsUtc = startsUtc,
            EndsUtc = endsUtc,
            Sites = sites,
            NoticeHours = noticeHours,
            MessageEn = Message(messageEn),
            MessageFr = Message(messageFr),
            MessageEs = Message(messageEs),
            PassHash = passHash,
            CreatedByUserId = createdByUserId
        };
    }

    // =====================================================
    // METHOD: STATUS
    // =====================================================
    public PlatformMaintenanceStatus StatusAt(DateTimeOffset utcNow)
    {
        if (CancelledUtc is not null)
        {
            return PlatformMaintenanceStatus.Cancelled;
        }

        if (utcNow >= EndsUtc)
        {
            return PlatformMaintenanceStatus.Finished;
        }

        if (utcNow >= StartsUtc)
        {
            return PlatformMaintenanceStatus.Running;
        }

        return utcNow >= AnnouncedUtc ? PlatformMaintenanceStatus.Announced : PlatformMaintenanceStatus.Scheduled;
    }

    public bool Closes(PlatformMaintenanceSites site)
    {
        return (Sites & site) != PlatformMaintenanceSites.None;
    }

    // =====================================================
    // METHOD: STOP
    // Before the start: cancelled. While running: ends now.
    // =====================================================
    public void Stop(Guid? userId, DateTimeOffset utcNow)
    {
        switch (StatusAt(utcNow))
        {
            case PlatformMaintenanceStatus.Finished:
            case PlatformMaintenanceStatus.Cancelled:
                throw new DomainException(PlatformMaintenanceErrors.AlreadyOver);
            case PlatformMaintenanceStatus.Running:
                EndsUtc = utcNow;
                EndedEarlyUtc = utcNow;
                break;
            default:
                CancelledUtc = utcNow;
                break;
        }

        StoppedByUserId = userId;
        MarkAsModified(utcNow);
    }

    private static string? Message(string? value)
    {
        string? message = string.IsNullOrWhiteSpace(value) ? null : value.Trim();

        if (message is { Length: > MessageMaxLength })
        {
            throw new DomainException(PlatformMaintenanceErrors.MessageTooLong);
        }

        return message;
    }
}
