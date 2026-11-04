using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.UseCases.Insurance.SaveOperationsInsurancePolicy;

// =========================================================
// FILE: SaveOperationsInsurancePolicyRequest.cs
//
// PURPOSE:
// Records a new insurance policy of the company, or changes one (Slice 5B, design section 3).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SaveOperationsInsurancePolicyRequest
{
    // None: a new policy.
    public Guid? PolicyId { get; init; }

    public OperationsPolicyTerms Terms { get; init; } = null!;
}
