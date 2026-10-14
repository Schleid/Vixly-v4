namespace Vixly.Platform.Application.UseCases.Subscriptions.ApplyPlatformPromotion;

// =========================================================
// FILE: ApplyPlatformPromotionRequest.cs
//
// PURPOSE:
// Applies an approved promotion code to a tenant's subscription (D-S20).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ApplyPlatformPromotionRequest
{
    public Guid PlatformTenantId { get; init; }

    public string Code { get; init; } = string.Empty;
}
