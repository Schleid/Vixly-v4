using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Plans.UpdatePlatformPlanDraft;

// =========================================================
// FILE: UpdatePlatformPlanDraftRequest.cs
//
// PURPOSE:
// Edits the draft version of a plan (D-S2, D-S7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class UpdatePlatformPlanDraftRequest
{
    public Guid PlatformPlanId { get; init; }

    public string CurrencyCode { get; init; } = string.Empty;

    public decimal MonthlyPricePerLicence { get; init; }

    public decimal AnnualPricePerLicence { get; init; }

    public decimal SetupFee { get; init; }

    public int MinimumLicences { get; init; }

    public int? TrialDays { get; init; }

    public int MaxLicensedDevices { get; init; }

    public int MaxWarehouses { get; init; }

    public IReadOnlyList<PlatformPlanFeature> Features { get; init; } = Array.Empty<PlatformPlanFeature>();
}
