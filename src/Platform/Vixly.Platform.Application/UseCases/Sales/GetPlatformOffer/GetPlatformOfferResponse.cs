using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.GetPlatformOffer;

// =========================================================
// FILE: GetPlatformOfferResponse.cs
//
// PURPOSE:
// Reads one offer link for its console page.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformOfferResponse
{
    private GetPlatformOfferResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformOfferDetail? Offer { get; private init; }

    public PlatformSalesAbilities Abilities { get; private init; } = PlatformSalesAbilities.None;

    public static GetPlatformOfferResponse Success(PlatformOfferDetail? offer, PlatformSalesAbilities abilities)
    {
        return new GetPlatformOfferResponse { Succeeded = true, Offer = offer, Abilities = abilities };
    }

    public static GetPlatformOfferResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformOfferResponse { Error = error };
    }
}
