using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Sales.ListPlatformOffers;

// =========================================================
// FILE: ListPlatformOffersResponse.cs
//
// PURPOSE:
// Pages the offer links for the console.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformOffersResponse
{
    private ListPlatformOffersResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformOfferListItem>? Offers { get; private init; }

    public PlatformSalesAbilities Abilities { get; private init; } = PlatformSalesAbilities.None;

    public static ListPlatformOffersResponse Success(PagedResult<PlatformOfferListItem>? offers, PlatformSalesAbilities abilities)
    {
        return new ListPlatformOffersResponse { Succeeded = true, Offers = offers, Abilities = abilities };
    }

    public static ListPlatformOffersResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformOffersResponse { Error = error };
    }
}
