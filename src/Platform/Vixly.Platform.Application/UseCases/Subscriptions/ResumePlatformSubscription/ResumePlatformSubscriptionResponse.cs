using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ResumePlatformSubscription;

// =========================================================
// FILE: ResumePlatformSubscriptionResponse.cs
//
// PURPOSE:
// Withdraws a requested cancellation before the period ends.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ResumePlatformSubscriptionResponse
{
    private ResumePlatformSubscriptionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ResumePlatformSubscriptionResponse Success()
    {
        return new ResumePlatformSubscriptionResponse { Succeeded = true };
    }

    public static ResumePlatformSubscriptionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ResumePlatformSubscriptionResponse { Error = error };
    }
}
