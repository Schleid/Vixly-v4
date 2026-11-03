using Vixly.Platform.Application.Maintenance;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Maintenance.ListPlatformMaintenanceWindows;

// =========================================================
// FILE: ListPlatformMaintenanceWindowsResponse.cs
//
// PURPOSE:
// Control's maintenance page: the windows to come and running, then the past ones.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListPlatformMaintenanceWindowsResponse
{
    private ListPlatformMaintenanceWindowsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformMaintenanceWindowItem> Open { get; private init; } = Array.Empty<PlatformMaintenanceWindowItem>();

    public PagedResult<PlatformMaintenanceWindowItem>? Past { get; private init; }

    public bool CanManage { get; private init; }

    public static ListPlatformMaintenanceWindowsResponse Success(IReadOnlyList<PlatformMaintenanceWindowItem> open, PagedResult<PlatformMaintenanceWindowItem>? past, bool canManage)
    {
        return new ListPlatformMaintenanceWindowsResponse { Succeeded = true, Open = open, Past = past, CanManage = canManage };
    }

    public static ListPlatformMaintenanceWindowsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListPlatformMaintenanceWindowsResponse { Error = error };
    }
}
