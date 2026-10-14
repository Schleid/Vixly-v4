using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Tenants.ListPlatformTenantLifecycleEvents;

// =========================================================
// FILE: ListPlatformTenantLifecycleEventsResponse.cs
//
// PURPOSE:
// Result of ListPlatformTenantLifecycleEvents.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformTenantLifecycleEventsResponse
{
    private ListPlatformTenantLifecycleEventsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformTenantEventItem>? Events { get; private init; }

    public static ListPlatformTenantLifecycleEventsResponse Success(PagedResult<PlatformTenantEventItem> events)
    {
        return new ListPlatformTenantLifecycleEventsResponse { Succeeded = true, Events = events };
    }

    public static ListPlatformTenantLifecycleEventsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformTenantLifecycleEventsResponse { Error = error };
    }
}
