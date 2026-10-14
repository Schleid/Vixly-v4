using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Sales.ReplacePlatformOfferLink;

// =========================================================
// FILE: ReplacePlatformOfferLinkResponse.cs
//
// PURPOSE:
// Issues a new link for an issued offer; the old link stops working.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReplacePlatformOfferLinkResponse
{
    private ReplacePlatformOfferLinkResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string? Link { get; private init; }

    public static ReplacePlatformOfferLinkResponse Success(string? link)
    {
        return new ReplacePlatformOfferLinkResponse { Succeeded = true, Link = link };
    }

    public static ReplacePlatformOfferLinkResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReplacePlatformOfferLinkResponse { Error = error };
    }
}
