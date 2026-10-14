using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.UpdatePlatformOffer;

// =========================================================
// FILE: UpdatePlatformOfferResponse.cs
//
// PURPOSE:
// Changes a draft or rejected offer; it becomes a draft again.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformOfferResponse
{
    private UpdatePlatformOfferResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformOfferResponse Success()
    {
        return new UpdatePlatformOfferResponse { Succeeded = true };
    }

    public static UpdatePlatformOfferResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformOfferResponse { Error = error };
    }
}
