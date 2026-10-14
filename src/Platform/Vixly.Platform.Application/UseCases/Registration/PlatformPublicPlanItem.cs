using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Registration;

// =========================================================
// FILE: PlatformPublicPlanItem.cs
//
// PURPOSE:
// One plan of the public catalogue (registration).
//
// RESPONSIBILITIES:
// - Carry what a prospect needs to choose: name, description, prices, limits, features and trial
//
// IMPORTANT NOTES:
// - Only active, public plans with a published version
// - Amounts are before tax (D-S9)
// =========================================================
public sealed record PlatformPublicPlanItem(
    string Code,
    string Name,
    string? Description,
    string CurrencyCode,
    decimal MonthlyPricePerLicence,
    decimal AnnualPricePerLicence,
    decimal SetupFee,
    int MinimumLicences,
    int MaxLicensedDevices,
    int MaxWarehouses,
    IReadOnlyList<PlatformPlanFeature> Features,
    int? TrialDays);
