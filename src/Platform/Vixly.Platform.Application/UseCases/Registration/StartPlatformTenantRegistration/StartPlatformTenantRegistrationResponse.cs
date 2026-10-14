using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Registration.StartPlatformTenantRegistration;

// =========================================================
// FILE: StartPlatformTenantRegistrationResponse.cs
//
// PURPOSE:
// Result of StartPlatformTenantRegistration.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StartPlatformTenantRegistrationResponse
{
    private StartPlatformTenantRegistrationResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string AccessToken { get; private init; } = string.Empty;

    public DateTimeOffset AccessExpiresUtc { get; private init; }

    public static StartPlatformTenantRegistrationResponse Success(string accessToken, DateTimeOffset accessExpiresUtc)
    {
        return new StartPlatformTenantRegistrationResponse { Succeeded = true, AccessToken = accessToken, AccessExpiresUtc = accessExpiresUtc };
    }

    public static StartPlatformTenantRegistrationResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StartPlatformTenantRegistrationResponse { Error = error };
    }
}
