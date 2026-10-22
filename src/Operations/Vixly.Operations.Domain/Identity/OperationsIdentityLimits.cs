namespace Vixly.Operations.Domain.Identity;

// =========================================================
// FILE: OperationsIdentityLimits.cs
//
// PURPOSE:
// Sizes and times of the Operations staff identity model (Slice 1).
//
// RESPONSIBILITIES:
// - Name every limit in one place
//
// IMPORTANT NOTES:
// - Idle sign-out 15 minutes and 12-hour sessions for every tenant (owner decision D-O6)
// - Lockout after 5 failed passwords for 15 minutes
// =========================================================
public static class OperationsIdentityLimits
{
    public const int NameMaxLength = 200;

    // Owner, 2026-10-21: the device and country of a sign-in.
    public const int DeviceNameMaxLength = 80;

    public const int DeviceNumberMaxLength = 12;

    public const int PasswordHashMaxLength = 200;

    public const int MaxFailedSignIns = 5;

    public static readonly TimeSpan SignInLockoutDuration = TimeSpan.FromMinutes(15);

    public const int EncryptedSecretMaxLength = 1000;

    public const int RecoveryCodeCount = 10;

    public const int TokenHashLength = 64;

    public const int IpAddressMaxLength = 45;

    public const int UserAgentMaxLength = 512;

    public static readonly TimeSpan SessionIdleTimeout = TimeSpan.FromMinutes(15);

    public static readonly TimeSpan SessionAbsoluteLifetime = TimeSpan.FromHours(12);

    public static readonly TimeSpan InvitationLifetime = TimeSpan.FromHours(72);

    public static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(60);
}
