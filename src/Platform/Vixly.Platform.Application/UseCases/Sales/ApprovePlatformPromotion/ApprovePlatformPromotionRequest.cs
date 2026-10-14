namespace Vixly.Platform.Application.UseCases.Sales.ApprovePlatformPromotion;

// =========================================================
// FILE: ApprovePlatformPromotionRequest.cs
//
// PURPOSE:
// Approves a submitted promotion; it goes live (D-S26).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ApprovePlatformPromotionRequest
{
    public Guid PromotionId { get; init; }

    public string? Reason { get; init; }
}
