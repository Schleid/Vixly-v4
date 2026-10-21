using Vixly.Operations.Application.Ports;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Devices.ListOperationsDevices;

// =========================================================
// FILE: ListOperationsDevicesResponse.cs
//
// PURPOSE:
// Lists the tenant's devices for the Owner (Slice 1C).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsDevicesResponse
{
    private ListOperationsDevicesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsDevice> Devices { get; private init; } = Array.Empty<OperationsDevice>();

    public static ListOperationsDevicesResponse Success(IReadOnlyList<OperationsDevice> devices)
    {
        return new ListOperationsDevicesResponse { Succeeded = true, Devices = devices };
    }

    public static ListOperationsDevicesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsDevicesResponse { Error = error };
    }
}
