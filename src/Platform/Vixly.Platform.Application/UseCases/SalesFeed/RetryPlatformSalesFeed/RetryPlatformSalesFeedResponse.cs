using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SalesFeed.RetryPlatformSalesFeed;

// =========================================================
// FILE: RetryPlatformSalesFeedResponse.cs
//
// PURPOSE:
// Starts the sales feed to TechOS again after a stop or a long retry.
//
// RESPONSIBILITIES:
// - Report success, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RetryPlatformSalesFeedResponse
{
    private RetryPlatformSalesFeedResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RetryPlatformSalesFeedResponse Success()
    {
        return new RetryPlatformSalesFeedResponse { Succeeded = true };
    }

    public static RetryPlatformSalesFeedResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RetryPlatformSalesFeedResponse { Error = error };
    }
}
