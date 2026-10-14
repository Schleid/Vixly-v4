using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.EndPlatformSubscriptionNow;

// =========================================================
// FILE: EndPlatformSubscriptionNowResponse.cs
//
// PURPOSE:
// Ends a subscription at once.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EndPlatformSubscriptionNowResponse
{
    private EndPlatformSubscriptionNowResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static EndPlatformSubscriptionNowResponse Success()
    {
        return new EndPlatformSubscriptionNowResponse { Succeeded = true };
    }

    public static EndPlatformSubscriptionNowResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EndPlatformSubscriptionNowResponse { Error = error };
    }
}
