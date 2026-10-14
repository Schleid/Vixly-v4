using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.RevokePlatformOffer;

// =========================================================
// FILE: RevokePlatformOfferResponse.cs
//
// PURPOSE:
// Revokes an offer so its link stops working (D-S21).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RevokePlatformOfferResponse
{
    private RevokePlatformOfferResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RevokePlatformOfferResponse Success()
    {
        return new RevokePlatformOfferResponse { Succeeded = true };
    }

    public static RevokePlatformOfferResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RevokePlatformOfferResponse { Error = error };
    }
}
