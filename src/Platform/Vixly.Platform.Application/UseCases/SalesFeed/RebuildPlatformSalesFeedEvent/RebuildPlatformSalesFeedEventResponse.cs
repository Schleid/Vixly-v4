using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SalesFeed.RebuildPlatformSalesFeedEvent;

// =========================================================
// FILE: RebuildPlatformSalesFeedEventResponse.cs
//
// PURPOSE:
// Result of RebuildPlatformSalesFeedEvent.
//
// RESPONSIBILITIES:
// - Report success, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RebuildPlatformSalesFeedEventResponse
{
    private RebuildPlatformSalesFeedEventResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RebuildPlatformSalesFeedEventResponse Success()
    {
        return new RebuildPlatformSalesFeedEventResponse { Succeeded = true };
    }

    public static RebuildPlatformSalesFeedEventResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RebuildPlatformSalesFeedEventResponse { Error = error };
    }
}
