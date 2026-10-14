namespace Vixly.Platform.Application.UseCases.Sales.RejectPlatformPromotion;

// =========================================================
// FILE: RejectPlatformPromotionRequest.cs
//
// PURPOSE:
// Rejects a submitted promotion with a reason (D-S26).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RejectPlatformPromotionRequest
{
    public Guid PromotionId { get; init; }

    public string? Reason { get; init; }
}
