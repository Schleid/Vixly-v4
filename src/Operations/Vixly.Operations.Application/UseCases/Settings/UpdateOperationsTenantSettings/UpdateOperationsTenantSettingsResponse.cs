using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Settings.UpdateOperationsTenantSettings;

// =========================================================
// FILE: UpdateOperationsTenantSettingsResponse.cs
//
// PURPOSE:
// Saves the tenant's settings (Slice 1E).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdateOperationsTenantSettingsResponse
{
    private UpdateOperationsTenantSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdateOperationsTenantSettingsResponse Success()
    {
        return new UpdateOperationsTenantSettingsResponse { Succeeded = true };
    }

    public static UpdateOperationsTenantSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdateOperationsTenantSettingsResponse { Error = error };
    }
}
