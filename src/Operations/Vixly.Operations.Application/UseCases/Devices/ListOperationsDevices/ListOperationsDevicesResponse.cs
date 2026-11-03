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

    // The company's licences (owner, 2026-10-21); none when Platform could not count them.
    public OperationsDeviceSeats? Seats { get; private init; }

    // Owner, R-6: the devices blocked after a sign-in from another country, until an administrator unblocks them.
    public IReadOnlyList<Vixly.Operations.Domain.Identity.OperationsDeviceBlock> Blocks { get; private init; } = Array.Empty<Vixly.Operations.Domain.Identity.OperationsDeviceBlock>();

    // Require MFA = No and no authenticator: the viewer confirms device actions with the password (owner, 2026-10-22).
    public bool PasswordConfirms { get; private init; }

    public static ListOperationsDevicesResponse Success(IReadOnlyList<OperationsDevice> devices, OperationsDeviceSeats? seats,
        IReadOnlyList<Vixly.Operations.Domain.Identity.OperationsDeviceBlock> blocks, bool passwordConfirms = false)
    {
        return new ListOperationsDevicesResponse { Succeeded = true, Devices = devices, Seats = seats, Blocks = blocks, PasswordConfirms = passwordConfirms };
    }

    public static ListOperationsDevicesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsDevicesResponse { Error = error };
    }
}
