using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsInsuranceRepository.cs
//
// PURPOSE:
// Stores the company's insurance policies, its liability terms and the insurance certificates of shipments (Slice 5B).
//
// RESPONSIBILITIES:
// - Add and read policies, liability terms and certificates
//
// IMPORTANT NOTES:
// - Scoped to the current tenant by the query filter
// =========================================================
public interface IOperationsInsuranceRepository
{
    void Add(OperationsInsurancePolicy policy);

    void Add(OperationsLiabilityTerms terms);

    void Add(OperationsInsuranceCertificate certificate);

    Task<OperationsInsurancePolicy?> GetPolicyAsync(Guid policyId, CancellationToken cancellationToken);

    // Newest first, archived ones last.
    Task<IReadOnlyList<OperationsInsurancePolicy>> ListPoliciesAsync(CancellationToken cancellationToken);

    Task<OperationsLiabilityTerms?> FindLiabilityAsync(OperationsTransportMode mode, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsLiabilityTerms>> ListLiabilityAsync(CancellationToken cancellationToken);

    // The shipment's certificate that is not void, if any.
    Task<OperationsInsuranceCertificate?> FindLiveCertificateAsync(Guid shipmentId, CancellationToken cancellationToken);

    // The live one, else the last voided (a cancelled shipment shows its void certificate).
    Task<OperationsInsuranceCertificate?> FindLatestCertificateAsync(Guid shipmentId, CancellationToken cancellationToken);
}
