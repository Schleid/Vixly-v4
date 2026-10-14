namespace Vixly.Platform.Application.UseCases.Subscriptions.RejectPlatformDiscount;

// =========================================================
// FILE: RejectPlatformDiscountRequest.cs
//
// PURPOSE:
// Rejects a discount waiting for the sales manager, with a reason (D-S24).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RejectPlatformDiscountRequest
{
    public Guid PlatformTenantId { get; init; }

    public Guid DiscountId { get; init; }

    public string? Reason { get; init; }
}
