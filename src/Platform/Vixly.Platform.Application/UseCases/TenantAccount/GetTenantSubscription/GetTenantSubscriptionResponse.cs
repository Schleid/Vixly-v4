using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetTenantSubscription;

// =========================================================
// FILE: GetTenantSubscriptionResponse.cs
//
// PURPOSE:
// The tenant's own subscription (call 19).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetTenantSubscriptionResponse
{
    private GetTenantSubscriptionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantSubscriptionView? Subscription { get; private init; }

    public static GetTenantSubscriptionResponse Success(PlatformTenantSubscriptionView? subscription)
    {
        return new GetTenantSubscriptionResponse { Succeeded = true, Subscription = subscription };
    }

    public static GetTenantSubscriptionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetTenantSubscriptionResponse { Error = error };
    }
}
