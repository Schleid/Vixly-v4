namespace Vixly.Platform.Application.UseCases.Sales.PausePlatformPromotion;

// =========================================================
// FILE: PausePlatformPromotionRequest.cs
//
// PURPOSE:
// Pauses an active promotion.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PausePlatformPromotionRequest
{
    public Guid PromotionId { get; init; }
}
