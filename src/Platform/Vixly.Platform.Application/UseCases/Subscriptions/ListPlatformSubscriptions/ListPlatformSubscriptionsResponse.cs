using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ListPlatformSubscriptions;

// =========================================================
// FILE: ListPlatformSubscriptionsResponse.cs
//
// PURPOSE:
// Pages every tenant subscription for the subscriptions screen.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformSubscriptionsResponse
{
    private ListPlatformSubscriptionsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformSubscriptionListItem>? Subscriptions { get; private init; }

    public static ListPlatformSubscriptionsResponse Success(PagedResult<PlatformSubscriptionListItem>? subscriptions)
    {
        return new ListPlatformSubscriptionsResponse { Succeeded = true, Subscriptions = subscriptions };
    }

    public static ListPlatformSubscriptionsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformSubscriptionsResponse { Error = error };
    }
}
