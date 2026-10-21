namespace Vixly.Operations.Application.UseCases.Insurance.ArchiveOperationsInsurancePolicy;

// =========================================================
// FILE: ArchiveOperationsInsurancePolicyRequest.cs
//
// PURPOSE:
// Archives an insurance policy the company no longer holds, or brings it back (Slice 5B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ArchiveOperationsInsurancePolicyRequest
{
    public Guid PolicyId { get; init; }

    public bool Archived { get; init; } = true;
}
