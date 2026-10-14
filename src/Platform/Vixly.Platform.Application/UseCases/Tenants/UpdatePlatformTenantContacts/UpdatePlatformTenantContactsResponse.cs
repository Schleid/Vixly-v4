using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.UpdatePlatformTenantContacts;

// =========================================================
// FILE: UpdatePlatformTenantContactsResponse.cs
//
// PURPOSE:
// Result of UpdatePlatformTenantContacts.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformTenantContactsResponse
{
    private UpdatePlatformTenantContactsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformTenantContactsResponse Success()
    {
        return new UpdatePlatformTenantContactsResponse { Succeeded = true };
    }

    public static UpdatePlatformTenantContactsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformTenantContactsResponse { Error = error };
    }
}
