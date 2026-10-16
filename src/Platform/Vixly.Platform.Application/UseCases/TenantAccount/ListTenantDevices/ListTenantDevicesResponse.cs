using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ListTenantDevices;

// =========================================================
// FILE: ListTenantDevicesResponse.cs
//
// PURPOSE:
// The tenant's devices, for its device page in Operations (call 24).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListTenantDevicesResponse
{
    private ListTenantDevicesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PagedResult<PlatformDeviceItem>? Devices { get; private init; }

    public static ListTenantDevicesResponse Success(PagedResult<PlatformDeviceItem>? devices)
    {
        return new ListTenantDevicesResponse { Succeeded = true, Devices = devices };
    }

    public static ListTenantDevicesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListTenantDevicesResponse { Error = error };
    }
}
