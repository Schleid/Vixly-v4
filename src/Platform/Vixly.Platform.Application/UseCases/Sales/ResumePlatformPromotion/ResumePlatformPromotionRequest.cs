namespace Vixly.Platform.Application.UseCases.Sales.ResumePlatformPromotion;

// =========================================================
// FILE: ResumePlatformPromotionRequest.cs
//
// PURPOSE:
// Resumes a paused promotion.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ResumePlatformPromotionRequest
{
    public Guid PromotionId { get; init; }
}
