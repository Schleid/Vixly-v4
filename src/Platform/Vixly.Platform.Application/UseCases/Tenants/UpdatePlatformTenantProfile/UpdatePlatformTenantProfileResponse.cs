using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.UpdatePlatformTenantProfile;

// =========================================================
// FILE: UpdatePlatformTenantProfileResponse.cs
//
// PURPOSE:
// Result of UpdatePlatformTenantProfile.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformTenantProfileResponse
{
    private UpdatePlatformTenantProfileResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformTenantProfileResponse Success()
    {
        return new UpdatePlatformTenantProfileResponse { Succeeded = true };
    }

    public static UpdatePlatformTenantProfileResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformTenantProfileResponse { Error = error };
    }
}
