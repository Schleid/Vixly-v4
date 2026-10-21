using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.RejectPlatformTenantChange;

// =========================================================
// FILE: RejectPlatformTenantChangeResponse.cs
//
// PURPOSE:
// Answer to rejecting a company change request (B12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RejectPlatformTenantChangeResponse
{
    private RejectPlatformTenantChangeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RejectPlatformTenantChangeResponse Success()
    {
        return new RejectPlatformTenantChangeResponse { Succeeded = true };
    }

    public static RejectPlatformTenantChangeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new RejectPlatformTenantChangeResponse { Error = error };
    }
}
