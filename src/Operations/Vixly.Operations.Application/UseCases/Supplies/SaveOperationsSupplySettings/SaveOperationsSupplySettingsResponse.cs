using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupplySettings;

// =========================================================
// FILE: SaveOperationsSupplySettingsResponse.cs
//
// PURPOSE:
// The company's supplies stock settings (Slice 5E, S-9): count the stock or not, and what the counter does when an item has none left.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsSupplySettingsResponse
{
    private SaveOperationsSupplySettingsResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsSupplySettingsResponse Success()
    {
        return new SaveOperationsSupplySettingsResponse { Succeeded = true };
    }

    public static SaveOperationsSupplySettingsResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsSupplySettingsResponse { Error = error };
    }
}
