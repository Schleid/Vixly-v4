using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registrations.CancelPlatformTenantRegistration;

// =========================================================
// FILE: CancelPlatformTenantRegistrationResponse.cs
//
// PURPOSE:
// Result of CancelPlatformTenantRegistration.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CancelPlatformTenantRegistrationResponse
{
    private CancelPlatformTenantRegistrationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static CancelPlatformTenantRegistrationResponse Success()
    {
        return new CancelPlatformTenantRegistrationResponse { Succeeded = true };
    }

    public static CancelPlatformTenantRegistrationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CancelPlatformTenantRegistrationResponse { Error = error };
    }
}
