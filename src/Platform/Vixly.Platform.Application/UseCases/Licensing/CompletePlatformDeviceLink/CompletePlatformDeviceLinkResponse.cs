using Vixly.Platform.Application.Licensing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.CompletePlatformDeviceLink;

// =========================================================
// FILE: CompletePlatformDeviceLinkResponse.cs
//
// PURPOSE:
// Enrols the device that opened a first-device or recovery link; the first device completes the registration and activates the tenant (D-L7, D-L8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CompletePlatformDeviceLinkResponse
{
    private CompletePlatformDeviceLinkResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformDeviceEnrolment? Enrolment { get; private init; }

    public static CompletePlatformDeviceLinkResponse Success(PlatformDeviceEnrolment? enrolment)
    {
        return new CompletePlatformDeviceLinkResponse { Succeeded = true, Enrolment = enrolment };
    }

    public static CompletePlatformDeviceLinkResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CompletePlatformDeviceLinkResponse { Error = error };
    }
}
