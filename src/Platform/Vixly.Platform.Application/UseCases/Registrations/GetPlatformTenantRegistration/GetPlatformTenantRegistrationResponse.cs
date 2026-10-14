using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registrations.GetPlatformTenantRegistration;

// =========================================================
// FILE: GetPlatformTenantRegistrationResponse.cs
//
// PURPOSE:
// Result of GetPlatformTenantRegistration.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformTenantRegistrationResponse
{
    private GetPlatformTenantRegistrationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantRegistrationDetail? Registration { get; private init; }

    public bool CanManage { get; private init; }

    public static GetPlatformTenantRegistrationResponse Success(PlatformTenantRegistrationDetail registration, bool canManage)
    {
        return new GetPlatformTenantRegistrationResponse { Succeeded = true, Registration = registration, CanManage = canManage };
    }

    public static GetPlatformTenantRegistrationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformTenantRegistrationResponse { Error = error };
    }
}
