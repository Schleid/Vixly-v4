using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.SetOperationsSupplyReorderLevel;

// =========================================================
// FILE: SetOperationsSupplyReorderLevelResponse.cs
//
// PURPOSE:
// The stock level at which a supply shows as low at a warehouse (Slice 5E, S-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SetOperationsSupplyReorderLevelResponse
{
    private SetOperationsSupplyReorderLevelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SetOperationsSupplyReorderLevelResponse Success()
    {
        return new SetOperationsSupplyReorderLevelResponse { Succeeded = true };
    }

    public static SetOperationsSupplyReorderLevelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SetOperationsSupplyReorderLevelResponse { Error = error };
    }
}
