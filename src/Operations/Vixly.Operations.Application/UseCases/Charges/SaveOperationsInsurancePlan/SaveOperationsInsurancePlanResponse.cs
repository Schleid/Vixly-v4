using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsInsurancePlan;

// =========================================================
// FILE: SaveOperationsInsurancePlanResponse.cs
//
// PURPOSE:
// Sets the company's insurance plan, from now or from a later day: a percentage with a minimum, or value bands (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsInsurancePlanResponse
{
    private SaveOperationsInsurancePlanResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsInsurancePlanResponse Success()
    {
        return new SaveOperationsInsurancePlanResponse { Succeeded = true };
    }

    public static SaveOperationsInsurancePlanResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsInsurancePlanResponse { Error = error };
    }
}
