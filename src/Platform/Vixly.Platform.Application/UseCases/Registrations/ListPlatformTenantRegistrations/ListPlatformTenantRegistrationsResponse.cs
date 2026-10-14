using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Registrations.ListPlatformTenantRegistrations;

// =========================================================
// FILE: ListPlatformTenantRegistrationsResponse.cs
//
// PURPOSE:
// Result of ListPlatformTenantRegistrations.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformTenantRegistrationsResponse
{
    private ListPlatformTenantRegistrationsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformTenantRegistrationListItem>? Registrations { get; private init; }

    public static ListPlatformTenantRegistrationsResponse Success(PagedResult<PlatformTenantRegistrationListItem> registrations)
    {
        return new ListPlatformTenantRegistrationsResponse { Succeeded = true, Registrations = registrations };
    }

    public static ListPlatformTenantRegistrationsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformTenantRegistrationsResponse { Error = error };
    }
}
