namespace Vixly.Platform.Infrastructure.Subscriptions;

// =========================================================
// FILE: PlatformSubscriptionOptions.cs
//
// PURPOSE:
// Settings of subscriptions.
//
// RESPONSIBILITIES:
// - Hold the read-only period of an unpaid subscription (D-S14)
//
// IMPORTANT NOTES:
// - Bound from the PlatformSubscriptions section
// =========================================================
public sealed class PlatformSubscriptionOptions
{
    public const string SectionName = "PlatformSubscriptions";

    public int UnpaidReadOnlyDays { get; set; } = 7;
}
