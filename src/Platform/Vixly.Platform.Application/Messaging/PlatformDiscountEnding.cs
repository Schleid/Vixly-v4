namespace Vixly.Platform.Application.Messaging;

// =========================================================
// FILE: PlatformDiscountEnding.cs
//
// PURPOSE:
// Payload of "discount ending" (D-S25).
//
// RESPONSIBILITIES:
// - Say whose discount ends and when
//
// IMPORTANT NOTES:
// - Written by the subscription clock once per discount
// =========================================================
public sealed record PlatformDiscountEnding(
    Guid PlatformSubscriptionId,
    Guid PlatformTenantId,
    Guid DiscountId,
    DateTimeOffset EndsUtc,
    DateTimeOffset OccurredUtc);
