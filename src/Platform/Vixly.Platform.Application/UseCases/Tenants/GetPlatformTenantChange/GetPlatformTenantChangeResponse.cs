using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.GetPlatformTenantChange;

// =========================================================
// FILE: GetPlatformTenantChangeResponse.cs
//
// PURPOSE:
// One company change request and whether the reader may decide it (B12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformTenantChangeResponse
{
    private GetPlatformTenantChangeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantChangeItem? Change { get; private init; }

    public bool CanDecide { get; private init; }

    public static GetPlatformTenantChangeResponse Success(PlatformTenantChangeItem? change, bool canDecide)
    {
        return new GetPlatformTenantChangeResponse { Succeeded = true, Change = change, CanDecide = canDecide };
    }

    public static GetPlatformTenantChangeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new GetPlatformTenantChangeResponse { Error = error };
    }
}
