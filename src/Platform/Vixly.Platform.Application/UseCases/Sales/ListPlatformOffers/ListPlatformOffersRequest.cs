using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.UseCases.Sales.ListPlatformOffers;

// =========================================================
// FILE: ListPlatformOffersRequest.cs
//
// PURPOSE:
// Pages the offer links for the console.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformOffersRequest
{
    public PlatformOfferStatus? Status { get; init; }

    public string? Search { get; init; }

    public string? Language { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
