using Vixly.Platform.Domain.Collections;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformCollectionCaseItem.cs
//
// PURPOSE:
// One collections case in a list.
//
// RESPONSIBILITIES:
// - Carry what the list and the case page show
//
// IMPORTANT NOTES:
// - Overdue holds one amount per currency
// =========================================================
public sealed record PlatformCollectionCaseItem(
    Guid CaseId,
    Guid PlatformTenantId,
    string TenantName,
    PlatformCollectionCaseStatus Status,
    PlatformCollectionStage Stage,
    DateTimeOffset OldestDueUtc,
    DateTimeOffset? PlannedBlockUtc,
    DateTimeOffset? BlockedUtc,
    DateTimeOffset? NoAccessUtc,
    DateTime? ExtendedThrough,
    int ExtensionsUsed,
    bool Held,
    PlatformSubscriptionStatus? SubscriptionStatus,
    IReadOnlyList<PlatformMoneyItem> Overdue,
    DateTimeOffset OpenedUtc,
    DateTimeOffset? ClosedUtc,
    PlatformCollectionCloseReason? CloseReason);
