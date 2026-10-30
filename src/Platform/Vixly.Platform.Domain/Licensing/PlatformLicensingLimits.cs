namespace Vixly.Platform.Domain.Licensing;

// =========================================================
// FILE: PlatformLicensingLimits.cs
//
// PURPOSE:
// Fixed limits of licensing: lifetimes, allowances and field lengths (D-L5 to D-L8).
//
// RESPONSIBILITIES:
// - Hold every lifetime and limit in one place
// - Compute the release allowance for a number of seats
//
// IMPORTANT NOTES:
// - Release allowance: 2 in any 30 days, or 25% of seats when that is more (owner, 2026-10-16)
// =========================================================
public static class PlatformLicensingLimits
{
    public const int MaxPending = 10;

    public const int NameMaxLength = 80;

    public const int ActorReferenceMaxLength = 100;

    public const int ActorNameMaxLength = 120;

    public const int PublicKeyMaxLength = 200;

    public const int HashLength = 64;

    // Owner support requests (D-O13): reason, identity check and decision note.
    public const int SupportTextMaxLength = 500;

    public const int NumberMaxLength = 12;

    // Where a device was registered from (owner, 2026-10-21).
    public const int IpMaxLength = 45;

    public const int UserAgentMaxLength = 512;

    public const int LinkMaxTries = 5;

    public const int LinksPerHour = 3;

    public const int AllowanceMinimum = 2;

    public const int AllowancePercent = 25;

    public static readonly TimeSpan PendingLifetime = TimeSpan.FromDays(7);

    public static readonly TimeSpan SecretRotation = TimeSpan.FromHours(24);

    public static readonly TimeSpan SecretGrace = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan ChallengeLifetime = TimeSpan.FromMinutes(2);

    public static readonly TimeSpan FirstDeviceLinkLifetime = TimeSpan.FromHours(24);

    public static readonly TimeSpan RecoveryLinkLifetime = TimeSpan.FromMinutes(60);

    public static readonly TimeSpan AllowanceWindow = TimeSpan.FromDays(30);

    public static readonly TimeSpan LastSeenEvery = TimeSpan.FromHours(1);

    public static int AllowanceFor(int seats)
    {
        return Math.Max(AllowanceMinimum, seats * AllowancePercent / 100);
    }

    public static bool ChosenByTenant(PlatformDeviceReason reason)
    {
        return reason is PlatformDeviceReason.Replaced or PlatformDeviceReason.LostOrStolen or PlatformDeviceReason.Broken
            or PlatformDeviceReason.StaffLeft or PlatformDeviceReason.Other;
    }
}
