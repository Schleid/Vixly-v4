using Vixly.Platform.Application.Licensing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Licensing.EnrolPlatformDevice;

// =========================================================
// FILE: EnrolPlatformDeviceResponse.cs
//
// PURPOSE:
// A tenant device asks to join: it waits for a tenant admin, holding no seat (D-L4, D-L5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class EnrolPlatformDeviceResponse
{
    private EnrolPlatformDeviceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformDeviceEnrolment? Enrolment { get; private init; }

    public static EnrolPlatformDeviceResponse Success(PlatformDeviceEnrolment? enrolment)
    {
        return new EnrolPlatformDeviceResponse { Succeeded = true, Enrolment = enrolment };
    }

    public static EnrolPlatformDeviceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new EnrolPlatformDeviceResponse { Error = error };
    }
}
