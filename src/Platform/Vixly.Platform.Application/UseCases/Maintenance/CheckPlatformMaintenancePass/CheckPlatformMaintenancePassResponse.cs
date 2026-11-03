using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Maintenance.CheckPlatformMaintenancePass;

// =========================================================
// FILE: CheckPlatformMaintenancePassResponse.cs
//
// PURPOSE:
// Checks a pass link Vixly IT opens to get through a running window.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CheckPlatformMaintenancePassResponse
{
    private CheckPlatformMaintenancePassResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public DateTimeOffset EndsUtc { get; private init; }

    public static CheckPlatformMaintenancePassResponse Success(DateTimeOffset endsUtc)
    {
        return new CheckPlatformMaintenancePassResponse { Succeeded = true, EndsUtc = endsUtc };
    }

    public static CheckPlatformMaintenancePassResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CheckPlatformMaintenancePassResponse { Error = error };
    }
}
