namespace Vixly.Platform.Application.UseCases.Sales.SubmitPlatformPromotion;

// =========================================================
// FILE: SubmitPlatformPromotionRequest.cs
//
// PURPOSE:
// Submits a promotion for CFO approval (D-S26).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SubmitPlatformPromotionRequest
{
    public Guid PromotionId { get; init; }
}
