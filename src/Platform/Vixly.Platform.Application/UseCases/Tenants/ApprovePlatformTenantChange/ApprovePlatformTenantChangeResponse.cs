using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.ApprovePlatformTenantChange;

// =========================================================
// FILE: ApprovePlatformTenantChangeResponse.cs
//
// PURPOSE:
// Answer to approveing a company change request (B12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ApprovePlatformTenantChangeResponse
{
    private ApprovePlatformTenantChangeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ApprovePlatformTenantChangeResponse Success()
    {
        return new ApprovePlatformTenantChangeResponse { Succeeded = true };
    }

    public static ApprovePlatformTenantChangeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new ApprovePlatformTenantChangeResponse { Error = error };
    }
}
