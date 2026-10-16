using Vixly.Platform.Domain.Licensing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.DenyPlatformDevice;

// =========================================================
// FILE: DenyPlatformDeviceResponse.cs
//
// PURPOSE:
// A tenant admin refuses a waiting device (D-L4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class DenyPlatformDeviceResponse
{
    private DenyPlatformDeviceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static DenyPlatformDeviceResponse Success()
    {
        return new DenyPlatformDeviceResponse { Succeeded = true };
    }

    public static DenyPlatformDeviceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new DenyPlatformDeviceResponse { Error = error };
    }
}
