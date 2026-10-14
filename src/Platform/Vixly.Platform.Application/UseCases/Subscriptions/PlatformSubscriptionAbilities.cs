namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionAbilities.cs
//
// PURPOSE:
// What the reader may do on a tenant subscription now.
//
// RESPONSIBILITIES:
// - Combine permissions with the subscription status
//
// IMPORTANT NOTES:
// - Used to show only allowed actions; every use case still checks (ADR-0001)
// =========================================================
public sealed record PlatformSubscriptionAbilities(
    bool CanStart,
    bool CanChange,
    bool CanExtendTrial,
    bool CanEndTrial,
    bool CanRecordPayment,
    bool CanCancel,
    bool CanResume,
    bool CanEndNow,
    bool CanCancelScheduledChange,
    bool CanOverrideLimits,
    bool CanGrantDiscount = false,
    bool CanDecideDiscount = false,
    bool CanManageCharges = false)
{
    public static PlatformSubscriptionAbilities None { get; } = new(false, false, false, false, false, false, false, false, false, false);
}
