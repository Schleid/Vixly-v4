using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Licensing.ListPlatformDevices;

// =========================================================
// FILE: ListPlatformDevicesResponse.cs
//
// PURPOSE:
// Lists devices, paged, by tenant, state and search; or the release queue (D-L15).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformDevicesResponse
{
    private ListPlatformDevicesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformDeviceItem>? Devices { get; private init; }

    public static ListPlatformDevicesResponse Success(PagedResult<PlatformDeviceItem>? devices)
    {
        return new ListPlatformDevicesResponse { Succeeded = true, Devices = devices };
    }

    public static ListPlatformDevicesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformDevicesResponse { Error = error };
    }
}
