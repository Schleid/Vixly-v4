using Vixly.Platform.Application.Maintenance;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Maintenance.GetPlatformMaintenanceForSite;

// =========================================================
// FILE: GetPlatformMaintenanceForSiteResponse.cs
//
// PURPOSE:
// What one site must show: windows announced or running that close it.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformMaintenanceForSiteResponse
{
    private GetPlatformMaintenanceForSiteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<PlatformMaintenanceWindowItem> Windows { get; private init; } = Array.Empty<PlatformMaintenanceWindowItem>();

    public static GetPlatformMaintenanceForSiteResponse Success(IReadOnlyList<PlatformMaintenanceWindowItem> windows)
    {
        return new GetPlatformMaintenanceForSiteResponse { Succeeded = true, Windows = windows };
    }

    public static GetPlatformMaintenanceForSiteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformMaintenanceForSiteResponse { Error = error };
    }
}
