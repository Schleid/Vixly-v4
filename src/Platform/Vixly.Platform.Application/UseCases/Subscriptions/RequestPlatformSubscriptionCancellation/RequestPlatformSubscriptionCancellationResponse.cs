using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.RequestPlatformSubscriptionCancellation;

// =========================================================
// FILE: RequestPlatformSubscriptionCancellationResponse.cs
//
// PURPOSE:
// Cancels at the period end, or at once for a trial or an unpaid subscription.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RequestPlatformSubscriptionCancellationResponse
{
    private RequestPlatformSubscriptionCancellationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RequestPlatformSubscriptionCancellationResponse Success()
    {
        return new RequestPlatformSubscriptionCancellationResponse { Succeeded = true };
    }

    public static RequestPlatformSubscriptionCancellationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RequestPlatformSubscriptionCancellationResponse { Error = error };
    }
}
