using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.ApprovePlatformOffer;

// =========================================================
// FILE: ApprovePlatformOfferResponse.cs
//
// PURPOSE:
// Approves an offer waiting for the sales manager and issues its link (D-S24).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ApprovePlatformOfferResponse
{
    private ApprovePlatformOfferResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string? Link { get; private init; }

    public static ApprovePlatformOfferResponse Success(string? link)
    {
        return new ApprovePlatformOfferResponse { Succeeded = true, Link = link };
    }

    public static ApprovePlatformOfferResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ApprovePlatformOfferResponse { Error = error };
    }
}
