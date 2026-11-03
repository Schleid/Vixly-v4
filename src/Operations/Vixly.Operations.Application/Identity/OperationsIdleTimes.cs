using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Identity;

// =========================================================
// FILE: OperationsIdleTimes.cs
//
// PURPOSE:
// The company's inactivity sign-out times: staff, customers, and the warning before (owner, 2026-10-27).
//
// RESPONSIBILITIES:
// - Carry the three times from the settings, or the defaults when the company has none
//
// IMPORTANT NOTES:
// - Before 2026-10-27 these were fixed in the code: staff 15 minutes, customers 60, the warning 2
// =========================================================
public sealed record OperationsIdleTimes(TimeSpan Staff, TimeSpan Customer, TimeSpan Warn)
{
    public static OperationsIdleTimes Default { get; } = new(
        TimeSpan.FromMinutes(OperationsSettingsLimits.StaffIdleMinutesDefault),
        TimeSpan.FromMinutes(OperationsSettingsLimits.CustomerIdleMinutesDefault),
        TimeSpan.FromMinutes(OperationsSettingsLimits.IdleWarnMinutesDefault));

    public static OperationsIdleTimes From(OperationsTenantSettings? settings) => settings is null
        ? Default
        : new(TimeSpan.FromMinutes(settings.StaffIdleMinutes), TimeSpan.FromMinutes(settings.CustomerIdleMinutes), TimeSpan.FromMinutes(settings.IdleWarnMinutes));
}
