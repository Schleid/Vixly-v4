namespace Vixly.Platform.Domain.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionStart.cs
//
// PURPOSE:
// A new subscription together with its first history event.
//
// RESPONSIBILITIES:
// - Carry the subscription and the event to save with it
//
// IMPORTANT NOTES:
// - Returned by PlatformSubscription.Start
// =========================================================
public sealed record PlatformSubscriptionStart(PlatformSubscription Subscription, PlatformSubscriptionEvent Event);
