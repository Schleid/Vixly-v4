namespace Vixly.Platform.Application.UseCases.Subscriptions.ApprovePlatformDiscount;

// =========================================================
// FILE: ApprovePlatformDiscountRequest.cs
//
// PURPOSE:
// Approves a discount waiting for the sales manager (D-S24).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ApprovePlatformDiscountRequest
{
    public Guid PlatformTenantId { get; init; }

    public Guid DiscountId { get; init; }

    public string? Reason { get; init; }
}
