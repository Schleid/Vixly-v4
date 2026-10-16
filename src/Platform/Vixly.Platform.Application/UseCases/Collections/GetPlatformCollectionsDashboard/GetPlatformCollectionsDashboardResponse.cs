using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Collections.GetPlatformCollectionsDashboard;

// =========================================================
// FILE: GetPlatformCollectionsDashboardResponse.cs
//
// PURPOSE:
// Reads the collections dashboard (D-C13).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformCollectionsDashboardResponse
{
    private GetPlatformCollectionsDashboardResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformCollectionsDashboard? Dashboard { get; private init; }

    public static GetPlatformCollectionsDashboardResponse Success(PlatformCollectionsDashboard? dashboard)
    {
        return new GetPlatformCollectionsDashboardResponse { Succeeded = true, Dashboard = dashboard };
    }

    public static GetPlatformCollectionsDashboardResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformCollectionsDashboardResponse { Error = error };
    }
}
