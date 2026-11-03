using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.SalesFeed.SendPlatformSalesFeedEventAgain;

// =========================================================
// FILE: SendPlatformSalesFeedEventAgainResponse.cs
//
// PURPOSE:
// Sends a parked sale event to TechOS again, unchanged.
//
// RESPONSIBILITIES:
// - Report success, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SendPlatformSalesFeedEventAgainResponse
{
    private SendPlatformSalesFeedEventAgainResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SendPlatformSalesFeedEventAgainResponse Success()
    {
        return new SendPlatformSalesFeedEventAgainResponse { Succeeded = true };
    }

    public static SendPlatformSalesFeedEventAgainResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SendPlatformSalesFeedEventAgainResponse { Error = error };
    }
}
