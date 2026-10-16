using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.DecidePlatformDeviceRelease;

// =========================================================
// FILE: DecidePlatformDeviceReleaseResponse.cs
//
// PURPOSE:
// Vixly staff approve or refuse a release over the tenant's allowance (D-L6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DecidePlatformDeviceReleaseResponse
{
    private DecidePlatformDeviceReleaseResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DecidePlatformDeviceReleaseResponse Success()
    {
        return new DecidePlatformDeviceReleaseResponse { Succeeded = true };
    }

    public static DecidePlatformDeviceReleaseResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DecidePlatformDeviceReleaseResponse { Error = error };
    }
}
