namespace Vixly.Platform.Application.UseCases.Sales.GetPlatformPromotion;

// =========================================================
// FILE: GetPlatformPromotionRequest.cs
//
// PURPOSE:
// Reads one promotion for its console page.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformPromotionRequest
{
    public Guid PromotionId { get; init; }

    public string? Language { get; init; }
}
