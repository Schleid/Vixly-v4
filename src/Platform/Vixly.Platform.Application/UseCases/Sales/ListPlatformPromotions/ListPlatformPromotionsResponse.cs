using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Sales.ListPlatformPromotions;

// =========================================================
// FILE: ListPlatformPromotionsResponse.cs
//
// PURPOSE:
// Pages the promotions for the console.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformPromotionsResponse
{
    private ListPlatformPromotionsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformPromotionListItem>? Promotions { get; private init; }

    public PlatformSalesAbilities Abilities { get; private init; } = PlatformSalesAbilities.None;

    public static ListPlatformPromotionsResponse Success(PagedResult<PlatformPromotionListItem>? promotions, PlatformSalesAbilities abilities)
    {
        return new ListPlatformPromotionsResponse { Succeeded = true, Promotions = promotions, Abilities = abilities };
    }

    public static ListPlatformPromotionsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformPromotionsResponse { Error = error };
    }
}
