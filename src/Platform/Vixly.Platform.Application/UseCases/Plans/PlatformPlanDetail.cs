using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Plans;

// =========================================================
// FILE: PlatformPlanDetail.cs
//
// PURPOSE:
// Everything the plan page shows.
//
// RESPONSIBILITIES:
// - Carry the plan, its translations, current version, draft, version history and subscriber count
//
// IMPORTANT NOTES:
// - Built from the aggregate by GetPlatformPlan
// =========================================================
public sealed record PlatformPlanDetail(
    Guid PlatformPlanId,
    string Code,
    string Name,
    PlatformPlanStatus Status,
    bool IsPublic,
    int DisplayOrder,
    IReadOnlyList<PlatformPlanTranslationItem> Translations,
    PlatformPlanVersionItem? Current,
    PlatformPlanVersionItem? Draft,
    IReadOnlyList<PlatformPlanVersionItem> Versions,
    int Subscribers,
    DateTimeOffset CreatedUtc);
