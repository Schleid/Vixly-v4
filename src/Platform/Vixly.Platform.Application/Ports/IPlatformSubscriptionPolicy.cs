namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformSubscriptionPolicy.cs
//
// PURPOSE:
// Configurable rules of subscriptions.
//
// RESPONSIBILITIES:
// - Give the read-only period of an unpaid subscription (D-S14)
//
// IMPORTANT NOTES:
// - Bound from Platform:Subscriptions configuration
// =========================================================
public interface IPlatformSubscriptionPolicy
{
    TimeSpan UnpaidReadOnlyPeriod { get; }
}
