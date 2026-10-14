using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.RemovePlatformTenantLimitOverride;

// =========================================================
// FILE: RemovePlatformTenantLimitOverrideResponse.cs
//
// PURPOSE:
// Removes a limit override; the plan value applies again.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemovePlatformTenantLimitOverrideResponse
{
    private RemovePlatformTenantLimitOverrideResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemovePlatformTenantLimitOverrideResponse Success()
    {
        return new RemovePlatformTenantLimitOverrideResponse { Succeeded = true };
    }

    public static RemovePlatformTenantLimitOverrideResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemovePlatformTenantLimitOverrideResponse { Error = error };
    }
}
