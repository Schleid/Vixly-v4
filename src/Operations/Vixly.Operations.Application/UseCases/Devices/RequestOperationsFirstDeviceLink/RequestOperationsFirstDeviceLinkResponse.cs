using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.RequestOperationsFirstDeviceLink;

// =========================================================
// FILE: RequestOperationsFirstDeviceLinkResponse.cs
//
// PURPOSE:
// Asks Platform to email the Owner the link that enrols their first computer and activates the tenant (Slice 1C).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RequestOperationsFirstDeviceLinkResponse
{
    private RequestOperationsFirstDeviceLinkResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RequestOperationsFirstDeviceLinkResponse Success()
    {
        return new RequestOperationsFirstDeviceLinkResponse { Succeeded = true };
    }

    public static RequestOperationsFirstDeviceLinkResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RequestOperationsFirstDeviceLinkResponse { Error = error };
    }
}
