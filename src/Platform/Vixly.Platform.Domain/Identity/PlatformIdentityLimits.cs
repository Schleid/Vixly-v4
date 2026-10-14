namespace Vixly.Platform.Domain.Identity;

// =========================================================
// FILE: PlatformIdentityLimits.cs
//
// PURPOSE:
// Field lengths and sign-in thresholds of the identity model.
//
// RESPONSIBILITIES:
// - Hold the limits the domain enforces and EF maps
//
// IMPORTANT NOTES:
// - Lockout values are carried from V3 (5 failures, 15
//   minutes), which operators already know
// - EF configurations read these values, so the database
//   column sizes and the domain checks never disagree
// - Session, step-up and token lifetimes are ADR-0006 values
// =========================================================
public static class PlatformIdentityLimits
{
    public const int NameMaxLength = 100;

    public const int EmployeeNumberMaxLength = 50;

    public const int ReasonMaxLength = 500;

    public const int DescriptionMaxLength = 500;

    public const int PasswordHashMaxLength = 200;

    public const int PermissionCodeMaxLength = 100;

    public const int SystemRoleKeyMaxLength = 50;

    public const int MaxFailedSignIns = 5;

    public static readonly TimeSpan SignInLockoutDuration = TimeSpan.FromMinutes(15);

    public const int PasskeyNameMaxLength = 100;

    public const int CredentialIdMaxLength = 1023;

    public const int PublicKeyMaxLength = 2048;

    public const int MaxPasskeysPerUser = 10;

    public const int EncryptedSecretMaxLength = 1000;

    public const int RecoveryCodeCount = 10;

    public const int TokenHashLength = 64;

    public const int IpAddressMaxLength = 45;

    public const int UserAgentMaxLength = 512;

    public static readonly TimeSpan SessionIdleTimeout = TimeSpan.FromMinutes(5);

    public static readonly TimeSpan SessionAbsoluteLifetime = TimeSpan.FromHours(12);

    public static readonly TimeSpan StepUpWindow = TimeSpan.FromMinutes(5);

    public const int StepUpMaxFailures = 5;

    public static readonly TimeSpan InvitationLifetime = TimeSpan.FromHours(72);

    public static readonly TimeSpan PasswordResetLifetime = TimeSpan.FromMinutes(60);
}
