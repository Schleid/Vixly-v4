using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Credit.GetOperationsCreditSettings;

// =========================================================
// FILE: GetOperationsCreditSettingsResponse.cs
//
// PURPOSE:
// The company's credit settings (Slice 5F step 2b, K-2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCreditSettingsResponse
{
    private GetOperationsCreditSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsTenantSettings Settings { get; private init; } = null!;

    public bool CanEdit { get; private init; }

    public static GetOperationsCreditSettingsResponse Success(OperationsTenantSettings settings, bool canEdit)
    {
        return new GetOperationsCreditSettingsResponse { Succeeded = true, Settings = settings, CanEdit = canEdit };
    }

    public static GetOperationsCreditSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCreditSettingsResponse { Error = error };
    }
}
