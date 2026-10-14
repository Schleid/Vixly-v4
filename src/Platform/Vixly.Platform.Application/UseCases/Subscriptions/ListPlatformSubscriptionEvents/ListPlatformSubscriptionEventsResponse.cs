using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ListPlatformSubscriptionEvents;

// =========================================================
// FILE: ListPlatformSubscriptionEventsResponse.cs
//
// PURPOSE:
// Pages a tenant's subscription history, newest first.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformSubscriptionEventsResponse
{
    private ListPlatformSubscriptionEventsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformSubscriptionEventItem>? Events { get; private init; }

    public static ListPlatformSubscriptionEventsResponse Success(PagedResult<PlatformSubscriptionEventItem>? events)
    {
        return new ListPlatformSubscriptionEventsResponse { Succeeded = true, Events = events };
    }

    public static ListPlatformSubscriptionEventsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformSubscriptionEventsResponse { Error = error };
    }
}
