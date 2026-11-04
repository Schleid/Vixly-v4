namespace Vixly.Operations.Application.UseCases.Insurance.GetOperationsInsurancePolicy;

// =========================================================
// FILE: GetOperationsInsurancePolicyRequest.cs
//
// PURPOSE:
// One of the company's insurance policies with its documents (Slice 5B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsInsurancePolicyRequest
{
    public Guid PolicyId { get; init; }
}
