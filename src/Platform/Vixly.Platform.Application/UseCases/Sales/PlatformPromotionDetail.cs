using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Sales;

// =========================================================
// FILE: PlatformPromotionDetail.cs
//
// PURPOSE:
// One promotion for its console page.
//
// RESPONSIBILITIES:
// - Carry the promotion with names, terms, eligibility, use and decision
//
// IMPORTANT NOTES:
// - People are given by name; ids stay internal
// =========================================================
public sealed record PlatformPromotionDetail(
    Guid PromotionId,
    string Code,
    string Name,
    IReadOnlyDictionary<string, string> Names,
    PlatformDiscountTermsItem Terms,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    IReadOnlyList<Guid> PlanIds,
    IReadOnlyList<string> PlanNames,
    PlatformSubscriptionTerm? Term,
    bool AtRegistration,
    bool ByStaff,
    int? RedemptionLimit,
    int Redemptions,
    PlatformPromotionStatus Status,
    string? DesignedByName,
    string? DecidedByName,
    DateTimeOffset? DecidedUtc,
    string? DecisionReason,
    DateTimeOffset CreatedUtc);
