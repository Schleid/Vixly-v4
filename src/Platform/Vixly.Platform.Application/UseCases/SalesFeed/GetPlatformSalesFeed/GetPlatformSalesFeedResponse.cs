using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SalesFeed.GetPlatformSalesFeed;

// =========================================================
// FILE: GetPlatformSalesFeedResponse.cs
//
// PURPOSE:
// Shows how the sales feed to TechOS stands.
//
// RESPONSIBILITIES:
// - Report success with the overview and whether the caller may act, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformSalesFeedResponse
{
    private GetPlatformSalesFeedResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformSalesFeedOverviewItem? Feed { get; private init; }

    public bool CanManage { get; private init; }

    public static GetPlatformSalesFeedResponse Success(PlatformSalesFeedOverviewItem feed, bool canManage)
    {
        ArgumentNullException.ThrowIfNull(feed);

        return new GetPlatformSalesFeedResponse { Succeeded = true, Feed = feed, CanManage = canManage };
    }

    public static GetPlatformSalesFeedResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformSalesFeedResponse { Error = error };
    }
}
