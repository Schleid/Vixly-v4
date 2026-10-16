using Vixly.Platform.Domain.Licensing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.ReleasePlatformDevice;

// =========================================================
// FILE: ReleasePlatformDeviceResponse.cs
//
// PURPOSE:
// A tenant admin frees a device's seat: at once within the allowance, otherwise Vixly reviews it; a lost device stops at once (D-L6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ReleasePlatformDeviceResponse
{
    private ReleasePlatformDeviceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformDeviceStatus Status { get; private init; }

    public bool AwaitsReview { get; private init; }

    public static ReleasePlatformDeviceResponse Success(PlatformDeviceStatus status, bool awaitsReview)
    {
        return new ReleasePlatformDeviceResponse { Succeeded = true, Status = status, AwaitsReview = awaitsReview };
    }

    public static ReleasePlatformDeviceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ReleasePlatformDeviceResponse { Error = error };
    }
}
