using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.SubmitPlatformOffer;

// =========================================================
// FILE: SubmitPlatformOfferResponse.cs
//
// PURPOSE:
// Issues the offer link at once when within the limits, otherwise asks the sales manager (D-S22, D-S24).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SubmitPlatformOfferResponse
{
    private SubmitPlatformOfferResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool NeedsApproval { get; private init; }

    public string? Link { get; private init; }

    public static SubmitPlatformOfferResponse Success(bool needsApproval, string? link)
    {
        return new SubmitPlatformOfferResponse { Succeeded = true, NeedsApproval = needsApproval, Link = link };
    }

    public static SubmitPlatformOfferResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SubmitPlatformOfferResponse { Error = error };
    }
}
