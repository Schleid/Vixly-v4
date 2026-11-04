using Vixly.Operations.Application.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Settings.GetOperationsTenantSettings;

// =========================================================
// FILE: GetOperationsTenantSettingsResponse.cs
//
// PURPOSE:
// Reads the tenant's settings, or the defaults before the first save.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsTenantSettingsResponse
{
    private GetOperationsTenantSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsSettingsView? View { get; private init; }

    public static GetOperationsTenantSettingsResponse Success(OperationsSettingsView? view)
    {
        return new GetOperationsTenantSettingsResponse { Succeeded = true, View = view };
    }

    public static GetOperationsTenantSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsTenantSettingsResponse { Error = error };
    }
}
