using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.SaveOperationsBundleSettings;

// =========================================================
// FILE: SaveOperationsBundleSettingsResponse.cs
//
// PURPOSE:
// Save the company's bundle settings (Slice 6A, D-1).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsBundleSettingsResponse
{
    private SaveOperationsBundleSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsBundleSettingsResponse Success()
    {
        return new SaveOperationsBundleSettingsResponse { Succeeded = true };
    }

    public static SaveOperationsBundleSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsBundleSettingsResponse { Error = error };
    }
}
