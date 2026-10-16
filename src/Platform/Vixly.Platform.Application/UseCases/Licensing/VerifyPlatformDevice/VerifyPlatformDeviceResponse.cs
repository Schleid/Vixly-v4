using Vixly.Platform.Application.Licensing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.VerifyPlatformDevice;

// =========================================================
// FILE: VerifyPlatformDeviceResponse.cs
//
// PURPOSE:
// Answers whether a device may be used now, from its signature or its rotating secret (D-L5, D-L10).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class VerifyPlatformDeviceResponse
{
    private VerifyPlatformDeviceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformDeviceCheck? Check { get; private init; }

    public static VerifyPlatformDeviceResponse Success(PlatformDeviceCheck? check)
    {
        return new VerifyPlatformDeviceResponse { Succeeded = true, Check = check };
    }

    public static VerifyPlatformDeviceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new VerifyPlatformDeviceResponse { Error = error };
    }
}
