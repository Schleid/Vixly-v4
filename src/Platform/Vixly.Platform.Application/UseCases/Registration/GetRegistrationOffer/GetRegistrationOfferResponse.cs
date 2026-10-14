using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registration.GetRegistrationOffer;

// =========================================================
// FILE: GetRegistrationOfferResponse.cs
//
// PURPOSE:
// Shows the terms of an offer link to the prospect (D-S21).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetRegistrationOfferResponse
{
    private GetRegistrationOfferResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformRegistrationOfferPreview? Offer { get; private init; }

    public static GetRegistrationOfferResponse Success(PlatformRegistrationOfferPreview? offer)
    {
        return new GetRegistrationOfferResponse { Succeeded = true, Offer = offer };
    }

    public static GetRegistrationOfferResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetRegistrationOfferResponse { Error = error };
    }
}
