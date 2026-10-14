using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ChangePlatformSubscription;

// =========================================================
// FILE: ChangePlatformSubscriptionResponse.cs
//
// PURPOSE:
// Changes plan, term or licences: upgrades and more licences now, the rest at the period end (D-S4, D-S5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangePlatformSubscriptionResponse
{
    private ChangePlatformSubscriptionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformSubscriptionQuoteItem? Quote { get; private init; }

    public static ChangePlatformSubscriptionResponse Success(PlatformSubscriptionQuoteItem? quote)
    {
        return new ChangePlatformSubscriptionResponse { Succeeded = true, Quote = quote };
    }

    public static ChangePlatformSubscriptionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangePlatformSubscriptionResponse { Error = error };
    }
}
