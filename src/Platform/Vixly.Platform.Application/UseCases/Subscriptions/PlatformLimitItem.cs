using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformLimitItem.cs
//
// PURPOSE:
// One limit of a tenant, with the plan value and any override (D-S8).
//
// RESPONSIBILITIES:
// - Carry the plan value, the open override and the value that applies
//
// IMPORTANT NOTES:
// - Effective = override while it applies, else the plan value
// =========================================================
public sealed record PlatformLimitItem(
    PlatformPlanLimitKind Kind,
    int PlanValue,
    int Effective,
    Guid? OverrideId,
    int? OverrideValue,
    string? OverrideReason,
    DateTimeOffset? OverrideEndsUtc,
    string? OverrideSetByName);
