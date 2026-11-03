namespace Vixly.Operations.Domain.Settings;

// =========================================================
// FILE: OperationsSettingsLimits.cs
//
// PURPOSE:
// Sizes for the tenant settings.
//
// RESPONSIBILITIES:
// - One place for column sizes and rules
//
// IMPORTANT NOTES:
// - Database sizes come from here
// =========================================================
public static class OperationsSettingsLimits
{
    public const int CurrencyCodeLength = 3;

    public const int PrefixMaxLength = 6;

    // Inactivity sign-out, set by the company (owner, 2026-10-27): staff 5 to 60 minutes, customers 15 to 240, the warning 1 to 5 minutes before.
    public const int StaffIdleMinutesDefault = 15;

    public const int StaffIdleMinutesMin = 5;

    public const int StaffIdleMinutesMax = 60;

    public const int CustomerIdleMinutesDefault = 60;

    public const int CustomerIdleMinutesMin = 15;

    public const int CustomerIdleMinutesMax = 240;

    public const int IdleWarnMinutesDefault = 2;

    public const int IdleWarnMinutesMin = 1;

    public const int IdleWarnMinutesMax = 5;
}
