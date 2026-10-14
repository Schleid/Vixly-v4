using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Tenants.AssignPlatformTenantAccountManager;

// =========================================================
// FILE: AssignPlatformTenantAccountManagerResponse.cs
//
// PURPOSE:
// Result of AssignPlatformTenantAccountManager.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AssignPlatformTenantAccountManagerResponse
{
    private AssignPlatformTenantAccountManagerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static AssignPlatformTenantAccountManagerResponse Success()
    {
        return new AssignPlatformTenantAccountManagerResponse { Succeeded = true };
    }

    public static AssignPlatformTenantAccountManagerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AssignPlatformTenantAccountManagerResponse { Error = error };
    }
}
