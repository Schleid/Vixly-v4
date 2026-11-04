using Vixly.Operations.Domain.Supplies;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupplyCount;

// =========================================================
// FILE: SaveOperationsSupplyCountResponse.cs
//
// PURPOSE:
// A stock count saved as the clerk goes, closed (each difference becomes a count correction) or cancelled (Slice 5E, S-7).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsSupplyCountResponse
{
    private SaveOperationsSupplyCountResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public int Corrections { get; private init; }

    public static SaveOperationsSupplyCountResponse Success(int corrections)
    {
        return new SaveOperationsSupplyCountResponse { Succeeded = true, Corrections = corrections };
    }

    public static SaveOperationsSupplyCountResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsSupplyCountResponse { Error = error };
    }
}
