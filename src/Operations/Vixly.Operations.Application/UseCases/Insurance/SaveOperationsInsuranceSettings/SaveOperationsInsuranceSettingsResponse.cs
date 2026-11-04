using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.SaveOperationsInsuranceSettings;

// =========================================================
// FILE: SaveOperationsInsuranceSettingsResponse.cs
//
// PURPOSE:
// The company's insurance settings: whether staff may change the insurance of an open shipment (Slice 5D, D-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsInsuranceSettingsResponse
{
    private SaveOperationsInsuranceSettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsInsuranceSettingsResponse Success()
    {
        return new SaveOperationsInsuranceSettingsResponse { Succeeded = true };
    }

    public static SaveOperationsInsuranceSettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsInsuranceSettingsResponse { Error = error };
    }
}
