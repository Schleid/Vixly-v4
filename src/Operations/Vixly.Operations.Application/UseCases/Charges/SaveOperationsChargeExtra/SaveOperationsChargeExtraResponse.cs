using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsChargeExtra;

// =========================================================
// FILE: SaveOperationsChargeExtraResponse.cs
//
// PURPOSE:
// Adds an extra charge or changes one; a new amount can be scheduled from a date (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsChargeExtraResponse
{
    private SaveOperationsChargeExtraResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid ExtraId { get; private init; }

    public static SaveOperationsChargeExtraResponse Success(Guid extraId)
    {
        return new SaveOperationsChargeExtraResponse { Succeeded = true, ExtraId = extraId };
    }

    public static SaveOperationsChargeExtraResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsChargeExtraResponse { Error = error };
    }
}
