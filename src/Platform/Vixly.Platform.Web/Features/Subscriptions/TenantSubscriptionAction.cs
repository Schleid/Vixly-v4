namespace Vixly.Platform.Web.Features.Subscriptions;

// =========================================================
// FILE: TenantSubscriptionAction.cs
//
// PURPOSE:
// The confirmed actions of a tenant subscription.
//
// RESPONSIBILITIES:
// - Name the actions that need a reason or step-up
//
// IMPORTANT NOTES:
// - Resume needs neither and is not listed
// =========================================================
public enum TenantSubscriptionAction
{
    Cancel = 0,
    EndNow = 1,
    RecordPayment = 2,
    ExtendTrial = 3,
    EndTrial = 4
}
