using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformTenantEntitlements.cs
//
// PURPOSE:
// What a tenant may use: features and effective limits.
//
// RESPONSIBILITIES:
// - Carry the subscription status, the access level, the features and the effective limits
//
// IMPORTANT NOTES:
// - Exposed to other planes through Contracts.Platform in Slice 9
// - Operations compares its own counts against the limits (sweep defect 6)
// =========================================================
public sealed record PlatformTenantEntitlements(
    Guid PlatformTenantId,
    PlatformSubscriptionStatus? Status,
    PlatformSubscriptionAccess Access,
    IReadOnlyList<PlatformPlanFeature> Features,
    IReadOnlyDictionary<PlatformPlanLimitKind, int> Limits,
    PlatformNonPaymentInfo? NonPayment = null,
    PlatformLegalInfo? Legal = null);
