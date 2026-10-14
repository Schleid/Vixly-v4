using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Subscriptions.SetPlatformTenantLimitOverride;

// =========================================================
// FILE: SetPlatformTenantLimitOverrideResponse.cs
//
// PURPOSE:
// Overrides one of a tenant's plan limits (D-S8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetPlatformTenantLimitOverrideResponse
{
    private SetPlatformTenantLimitOverrideResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid OverrideId { get; private init; }

    public static SetPlatformTenantLimitOverrideResponse Success(Guid overrideId)
    {
        return new SetPlatformTenantLimitOverrideResponse { Succeeded = true, OverrideId = overrideId };
    }

    public static SetPlatformTenantLimitOverrideResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetPlatformTenantLimitOverrideResponse { Error = error };
    }
}
