using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.PreviewPlatformSubscriptionChange;

// =========================================================
// FILE: PreviewPlatformSubscriptionChangeResponse.cs
//
// PURPOSE:
// Shows what a plan, term or licence change would do and cost (D-S4, D-S5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PreviewPlatformSubscriptionChangeResponse
{
    private PreviewPlatformSubscriptionChangeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformSubscriptionQuoteItem? Quote { get; private init; }

    public static PreviewPlatformSubscriptionChangeResponse Success(PlatformSubscriptionQuoteItem? quote)
    {
        return new PreviewPlatformSubscriptionChangeResponse { Succeeded = true, Quote = quote };
    }

    public static PreviewPlatformSubscriptionChangeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PreviewPlatformSubscriptionChangeResponse { Error = error };
    }
}
