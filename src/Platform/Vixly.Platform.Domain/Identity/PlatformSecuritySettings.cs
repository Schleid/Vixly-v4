using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformSecuritySettings.cs
//
// PURPOSE:
// Control's sign-in rule for Vixly staff: whether a second
// factor is required (owner, 2026-10-22: "Require MFA: Yes/No").
//
// RESPONSIBILITIES:
// - Hold Require MFA, Yes by default
// - Hold the inactivity sign-out and the warning before it
//   (owner, 2026-10-27; it was 5 minutes, fixed in the code)
// - Record who changed it
//
// IMPORTANT NOTES:
// - One row (Key "default"); no row means Yes
// - With No, nobody is made to set up a second factor; people
//   who have one still use it
// =========================================================
public sealed class PlatformSecuritySettings : AuditableEntity
{
    public const string SingletonKey = "default";

    private PlatformSecuritySettings()
    {
        Key = SingletonKey;
        RequireMfa = true;
        IdleMinutes = (int)PlatformIdentityLimits.SessionIdleTimeout.TotalMinutes;
        IdleWarnMinutes = PlatformIdentityLimits.IdleWarnMinutesDefault;
    }

    private PlatformSecuritySettings(DateTimeOffset utcNow)
        : base(utcNow)
    {
        Key = SingletonKey;
        RequireMfa = true;
        IdleMinutes = (int)PlatformIdentityLimits.SessionIdleTimeout.TotalMinutes;
        IdleWarnMinutes = PlatformIdentityLimits.IdleWarnMinutesDefault;
    }

    public string Key { get; private set; }

    public bool RequireMfa { get; private set; }

    public int IdleMinutes { get; private set; }

    public int IdleWarnMinutes { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public static PlatformSecuritySettings Default(DateTimeOffset utcNow)
    {
        return new PlatformSecuritySettings(utcNow);
    }

    public static bool Requires(PlatformSecuritySettings? settings) => settings?.RequireMfa ?? true;

    public void ChangeIdle(int idleMinutes, int warnMinutes, Guid actorUserId, DateTimeOffset utcNow)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformIdentityErrors.UserRequired);
        }

        if (idleMinutes is < PlatformIdentityLimits.IdleMinutesMin or > PlatformIdentityLimits.IdleMinutesMax)
        {
            throw new DomainException(PlatformIdentityErrors.IdleInvalid);
        }

        // The warning always comes before the sign-out.
        if (warnMinutes is < PlatformIdentityLimits.IdleWarnMinutesMin or > PlatformIdentityLimits.IdleWarnMinutesMax || warnMinutes >= idleMinutes)
        {
            throw new DomainException(PlatformIdentityErrors.IdleWarnInvalid);
        }

        (IdleMinutes, IdleWarnMinutes, UpdatedByUserId) = (idleMinutes, warnMinutes, actorUserId);
        MarkAsModified(utcNow);
    }

    public void ChangeRequireMfa(bool requireMfa, Guid actorUserId, DateTimeOffset utcNow)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformIdentityErrors.UserRequired);
        }

        RequireMfa = requireMfa;
        UpdatedByUserId = actorUserId;
        MarkAsModified(utcNow);
    }
}
