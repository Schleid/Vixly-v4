using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Credit.SaveOperationsCreditSettings;

// =========================================================
// FILE: SaveOperationsCreditSettingsResponse.cs
//
// PURPOSE:
// Save the company's credit settings (Slice 5F step 2b, K-2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsCreditSettingsResponse
{
    private SaveOperationsCreditSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsCreditSettingsResponse Success()
    {
        return new SaveOperationsCreditSettingsResponse { Succeeded = true };
    }

    public static SaveOperationsCreditSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsCreditSettingsResponse { Error = error };
    }
}
