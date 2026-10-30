using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterInsurance;

// =========================================================
// FILE: ChangeOperationsCounterInsuranceResponse.cs
//
// PURPOSE:
// At the counter, the customer takes another insurance product, or none, for an open shipment (Slice 5D, D-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ChangeOperationsCounterInsuranceResponse
{
    private ChangeOperationsCounterInsuranceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static ChangeOperationsCounterInsuranceResponse Success()
    {
        return new ChangeOperationsCounterInsuranceResponse { Succeeded = true };
    }

    public static ChangeOperationsCounterInsuranceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ChangeOperationsCounterInsuranceResponse { Error = error };
    }
}
