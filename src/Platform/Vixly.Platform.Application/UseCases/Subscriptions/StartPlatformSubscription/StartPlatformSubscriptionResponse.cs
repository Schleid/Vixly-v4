using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.StartPlatformSubscription;

// =========================================================
// FILE: StartPlatformSubscriptionResponse.cs
//
// PURPOSE:
// Starts a subscription for a tenant without one (D-S15).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StartPlatformSubscriptionResponse
{
    private StartPlatformSubscriptionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PlatformSubscriptionId { get; private init; }

    public static StartPlatformSubscriptionResponse Success(Guid platformSubscriptionId)
    {
        return new StartPlatformSubscriptionResponse { Succeeded = true, PlatformSubscriptionId = platformSubscriptionId };
    }

    public static StartPlatformSubscriptionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StartPlatformSubscriptionResponse { Error = error };
    }
}
