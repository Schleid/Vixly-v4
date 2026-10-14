using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformPlanOption.cs
//
// PURPOSE:
// A plan a subscription can move to or start on.
//
// RESPONSIBILITIES:
// - Carry prices, minimum licences, the effective licence ceiling and the trial
//
// IMPORTANT NOTES:
// - Only plans that can be chosen are listed
// =========================================================
public sealed record PlatformPlanOption(
    Guid PlatformPlanId,
    string Code,
    string Name,
    string CurrencyCode,
    decimal MonthlyPricePerLicence,
    decimal AnnualPricePerLicence,
    decimal SetupFee,
    int MinimumLicences,
    int MaxLicences,
    int? TrialDays);
