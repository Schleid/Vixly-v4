namespace Vixly.Platform.Application.UseCases.Sales.EndPlatformPromotion;

// =========================================================
// FILE: EndPlatformPromotionRequest.cs
//
// PURPOSE:
// Ends a promotion for good.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class EndPlatformPromotionRequest
{
    public Guid PromotionId { get; init; }
}
