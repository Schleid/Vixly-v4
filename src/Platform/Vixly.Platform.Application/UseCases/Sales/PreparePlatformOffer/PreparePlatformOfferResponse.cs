using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.PreparePlatformOffer;

// =========================================================
// FILE: PreparePlatformOfferResponse.cs
//
// PURPOSE:
// Prepares an offer link with the agreed terms, as a draft (D-S21).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PreparePlatformOfferResponse
{
    private PreparePlatformOfferResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid OfferId { get; private init; }

    public static PreparePlatformOfferResponse Success(Guid offerId)
    {
        return new PreparePlatformOfferResponse { Succeeded = true, OfferId = offerId };
    }

    public static PreparePlatformOfferResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PreparePlatformOfferResponse { Error = error };
    }
}
