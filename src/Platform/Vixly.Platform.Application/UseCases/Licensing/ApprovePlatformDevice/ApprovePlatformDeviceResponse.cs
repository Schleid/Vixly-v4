using Vixly.Platform.Domain.Licensing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.ApprovePlatformDevice;

// =========================================================
// FILE: ApprovePlatformDeviceResponse.cs
//
// PURPOSE:
// A tenant admin approves a waiting device; it takes a free seat (D-L2, D-L4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ApprovePlatformDeviceResponse
{
    private ApprovePlatformDeviceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ApprovePlatformDeviceResponse Success()
    {
        return new ApprovePlatformDeviceResponse { Succeeded = true };
    }

    public static ApprovePlatformDeviceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ApprovePlatformDeviceResponse { Error = error };
    }
}
