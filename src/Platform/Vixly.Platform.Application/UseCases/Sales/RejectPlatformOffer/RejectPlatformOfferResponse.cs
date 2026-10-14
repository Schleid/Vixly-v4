using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.RejectPlatformOffer;

// =========================================================
// FILE: RejectPlatformOfferResponse.cs
//
// PURPOSE:
// Rejects an offer waiting for the sales manager, with a reason (D-S24).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RejectPlatformOfferResponse
{
    private RejectPlatformOfferResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RejectPlatformOfferResponse Success()
    {
        return new RejectPlatformOfferResponse { Succeeded = true };
    }

    public static RejectPlatformOfferResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RejectPlatformOfferResponse { Error = error };
    }
}
