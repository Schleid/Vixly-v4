using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ChangeTenantSubscription;

// =========================================================
// FILE: ChangeTenantSubscriptionResponse.cs
//
// PURPOSE:
// The tenant changes its plan, term or licences by itself (call 21).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeTenantSubscriptionResponse
{
    private ChangeTenantSubscriptionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformSubscriptionQuoteItem? Quote { get; private init; }

    public static ChangeTenantSubscriptionResponse Success(PlatformSubscriptionQuoteItem? quote)
    {
        return new ChangeTenantSubscriptionResponse { Succeeded = true, Quote = quote };
    }

    public static ChangeTenantSubscriptionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeTenantSubscriptionResponse { Error = error };
    }
}
