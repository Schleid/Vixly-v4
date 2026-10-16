using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.PreviewTenantSubscriptionChange;

// =========================================================
// FILE: PreviewTenantSubscriptionChangeResponse.cs
//
// PURPOSE:
// What a change of plan, term or licences would cost the tenant, before it confirms (call 21).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PreviewTenantSubscriptionChangeResponse
{
    private PreviewTenantSubscriptionChangeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformSubscriptionQuoteItem? Quote { get; private init; }

    public static PreviewTenantSubscriptionChangeResponse Success(PlatformSubscriptionQuoteItem? quote)
    {
        return new PreviewTenantSubscriptionChangeResponse { Succeeded = true, Quote = quote };
    }

    public static PreviewTenantSubscriptionChangeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PreviewTenantSubscriptionChangeResponse { Error = error };
    }
}
