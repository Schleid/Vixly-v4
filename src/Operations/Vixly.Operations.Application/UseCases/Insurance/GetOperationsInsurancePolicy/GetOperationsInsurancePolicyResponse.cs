using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.GetOperationsInsurancePolicy;

// =========================================================
// FILE: GetOperationsInsurancePolicyResponse.cs
//
// PURPOSE:
// One of the company's insurance policies with its documents (Slice 5B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsInsurancePolicyResponse
{
    private GetOperationsInsurancePolicyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsInsurancePolicy? Policy { get; private init; }

    public IReadOnlyList<OperationsInsurancePlan> Covers { get; private init; } = Array.Empty<OperationsInsurancePlan>();

    public DateOnly Today { get; private init; }

    public bool CanManage { get; private init; }

    public static GetOperationsInsurancePolicyResponse Success(OperationsInsurancePolicy? policy, IReadOnlyList<OperationsInsurancePlan> covers, DateOnly today, bool canManage)
    {
        return new GetOperationsInsurancePolicyResponse { Succeeded = true, Policy = policy, Covers = covers, Today = today, CanManage = canManage };
    }

    public static GetOperationsInsurancePolicyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsInsurancePolicyResponse { Error = error };
    }
}
