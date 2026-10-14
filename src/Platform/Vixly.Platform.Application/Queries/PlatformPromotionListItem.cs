using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformPromotionListItem.cs
//
// PURPOSE:
// One promotion in the console list.
//
// RESPONSIBILITIES:
// - Carry one row for a console list
//
// IMPORTANT NOTES:
// - Read model; built by Infrastructure
// =========================================================
public sealed record PlatformPromotionListItem(
    Guid PromotionId,
    string Code,
    string Name,
    PlatformPromotionStatus Status,
    PlatformDiscountKind Kind,
    decimal Value,
    PlatformDiscountDuration Duration,
    int? Periods,
    DateTimeOffset? EndsUtc,
    DateTimeOffset ValidFromUtc,
    DateTimeOffset? ValidToUtc,
    bool AtRegistration,
    bool ByStaff,
    int? RedemptionLimit,
    int Redemptions);
