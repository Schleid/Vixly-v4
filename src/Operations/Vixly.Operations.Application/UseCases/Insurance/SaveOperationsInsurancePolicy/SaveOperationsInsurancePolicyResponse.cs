using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.SaveOperationsInsurancePolicy;

// =========================================================
// FILE: SaveOperationsInsurancePolicyResponse.cs
//
// PURPOSE:
// Records a new insurance policy of the company, or changes one (Slice 5B, design section 3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsInsurancePolicyResponse
{
    private SaveOperationsInsurancePolicyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid PolicyId { get; private init; }

    public static SaveOperationsInsurancePolicyResponse Success(Guid policyId)
    {
        return new SaveOperationsInsurancePolicyResponse { Succeeded = true, PolicyId = policyId };
    }

    public static SaveOperationsInsurancePolicyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsInsurancePolicyResponse { Error = error };
    }
}
