using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.ListOperationsInsurance;

// =========================================================
// FILE: ListOperationsInsuranceResponse.cs
//
// PURPOSE:
// The insurance page: the company's policies, the cover it sells per mode and the policy each sits under, its liability terms (Slice 5B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class ListOperationsInsuranceResponse
{
    private ListOperationsInsuranceResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsInsurancePolicy> Policies { get; private init; } = Array.Empty<OperationsInsurancePolicy>();

    public IReadOnlyList<OperationsInsurancePlan> Covers { get; private init; } = Array.Empty<OperationsInsurancePlan>();

    public IReadOnlyList<OperationsLiabilityView> Liability { get; private init; } = Array.Empty<OperationsLiabilityView>();

    public DateOnly Today { get; private init; }

    public bool CanManage { get; private init; }

    public OperationsTenantSettings? Settings { get; private init; }

    public static ListOperationsInsuranceResponse Success(IReadOnlyList<OperationsInsurancePolicy> policies, IReadOnlyList<OperationsInsurancePlan> covers, IReadOnlyList<OperationsLiabilityView> liability, DateOnly today, bool canManage, OperationsTenantSettings? settings)
    {
        return new ListOperationsInsuranceResponse { Succeeded = true, Policies = policies, Covers = covers, Liability = liability, Today = today, CanManage = canManage, Settings = settings };
    }

    public static ListOperationsInsuranceResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new ListOperationsInsuranceResponse { Error = error };
    }
}
