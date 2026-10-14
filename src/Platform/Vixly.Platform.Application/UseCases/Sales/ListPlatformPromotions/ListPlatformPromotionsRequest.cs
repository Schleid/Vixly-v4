using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.UseCases.Sales.ListPlatformPromotions;

// =========================================================
// FILE: ListPlatformPromotionsRequest.cs
//
// PURPOSE:
// Pages the promotions for the console.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformPromotionsRequest
{
    public PlatformPromotionStatus? Status { get; init; }

    public string? Search { get; init; }

    public string? Language { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
