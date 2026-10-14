using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Plans;

// =========================================================
// FILE: PlatformPlanVersionItem.cs
//
// PURPOSE:
// One version of a plan, as the console shows it.
//
// RESPONSIBILITIES:
// - Carry prices, setup fee, currency, minimum licences, trial, limits, features and publication
//
// IMPORTANT NOTES:
// - Amounts are before tax (D-S9)
// =========================================================
public sealed record PlatformPlanVersionItem(
    Guid VersionId,
    int Number,
    PlatformPlanVersionStatus Status,
    string CurrencyCode,
    decimal MonthlyPricePerLicence,
    decimal AnnualPricePerLicence,
    decimal SetupFee,
    int MinimumLicences,
    int? TrialDays,
    int MaxLicensedDevices,
    int MaxWarehouses,
    IReadOnlyList<PlatformPlanFeature> Features,
    DateTimeOffset? PublishedUtc,
    string? PublishedByName,
    DateTimeOffset? SubmittedUtc = null,
    string? RejectionReason = null)
{
    public static PlatformPlanVersionItem From(PlatformPlanVersion version, string? publishedByName)
    {
        ArgumentNullException.ThrowIfNull(version);

        return new PlatformPlanVersionItem(
            version.Id,
            version.Number,
            version.Status,
            version.CurrencyCode,
            version.MonthlyPricePerLicence,
            version.AnnualPricePerLicence,
            version.SetupFee,
            version.MinimumLicences,
            version.TrialDays,
            version.Limit(PlatformPlanLimitKind.MaxLicensedDevices),
            version.Limit(PlatformPlanLimitKind.MaxWarehouses),
            version.Features.Select(item => item.Feature).OrderBy(feature => feature).ToArray(),
            version.PublishedUtc,
            publishedByName,
            version.SubmittedUtc,
            version.RejectionReason);
    }
}
