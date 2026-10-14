using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformPlanListItem.cs
//
// PURPOSE:
// One row of the plans list.
//
// RESPONSIBILITIES:
// - Carry what the list shows about a plan
//
// IMPORTANT NOTES:
// - Name in the reader language, falling back to English
// =========================================================
public sealed record PlatformPlanListItem(
    Guid PlatformPlanId,
    string Code,
    string Name,
    PlatformPlanStatus Status,
    bool IsPublic,
    int DisplayOrder,
    int? CurrentVersionNumber,
    bool HasDraft,
    string CurrencyCode,
    decimal? MonthlyPricePerLicence,
    decimal? AnnualPricePerLicence,
    int Subscribers);
