namespace Vixly.Platform.Domain.Legal;

// =========================================================
// FILE: PlatformLegalLimits.cs
//
// PURPOSE:
// Sizes and periods for legal documents, acceptances and reminders.
//
// RESPONSIBILITIES:
// - Field lengths, file size, notice period, cooldown
//
// IMPORTANT NOTES:
// - Notice period and grace for new tenants are 30 days (D-Lg8, D-Lg10)
// =========================================================
public static class PlatformLegalLimits
{
    public const int VersionMaxLength = 20;

    public const int SummaryMaxLength = 2000;

    public const int MaxFileMegabytes = 10;

    public const int HashLength = 64;

    public const int ActorReferenceMaxLength = 100;

    public const int ActorNameMaxLength = 120;

    public const int RoleMaxLength = 40;

    public const int IpAddressMaxLength = 45;

    public const int UserAgentMaxLength = 400;

    public const int ReasonMaxLength = 500;

    public const int MaxEvidenceMegabytes = 5;

    public const int NoticeDays = 30;

    public const int NewTenantDays = 30;

    public static TimeSpan ManualReminderCooldown { get; } = TimeSpan.FromHours(24);

    public const string OwnerRole = "owner";
}
