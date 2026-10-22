using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsInsuranceRepository.cs
//
// PURPOSE:
// EF Core store for the company's insurance policies, liability terms and certificates (Slice 5B).
//
// RESPONSIBILITIES:
// - Implement IOperationsInsuranceRepository
//
// IMPORTANT NOTES:
// - The tenant query filter scopes every read
// =========================================================
internal sealed class OperationsInsuranceRepository : IOperationsInsuranceRepository
{
    private const int MaxPolicies = 500;

    private readonly OperationsDbContext _context;

    public OperationsInsuranceRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsInsurancePolicy policy) => _context.InsurancePolicies.Add(policy ?? throw new ArgumentNullException(nameof(policy)));

    public void Add(OperationsLiabilityTerms terms) => _context.LiabilityTerms.Add(terms ?? throw new ArgumentNullException(nameof(terms)));

    public void Add(OperationsInsuranceCertificate certificate) => _context.InsuranceCertificates.Add(certificate ?? throw new ArgumentNullException(nameof(certificate)));

    public Task<OperationsInsurancePolicy?> GetPolicyAsync(Guid policyId, CancellationToken cancellationToken) =>
        _context.InsurancePolicies.FirstOrDefaultAsync(item => item.Id == policyId, cancellationToken);

    public async Task<IReadOnlyList<OperationsInsurancePolicy>> ListPoliciesAsync(CancellationToken cancellationToken) =>
        await _context.InsurancePolicies.AsNoTracking().OrderBy(item => item.IsArchived).ThenByDescending(item => item.CreatedUtc).Take(MaxPolicies).ToListAsync(cancellationToken);

    public Task<OperationsLiabilityTerms?> FindLiabilityAsync(OperationsTransportMode mode, CancellationToken cancellationToken) =>
        _context.LiabilityTerms.FirstOrDefaultAsync(item => item.Mode == mode, cancellationToken);

    public async Task<IReadOnlyList<OperationsLiabilityTerms>> ListLiabilityAsync(CancellationToken cancellationToken) =>
        await _context.LiabilityTerms.AsNoTracking().ToListAsync(cancellationToken);

    public async Task<OperationsInsuranceCertificate?> FindLiveCertificateAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        _context.InsuranceCertificates.Local.FirstOrDefault(item => item.ShipmentId == shipmentId && item.Status != OperationsCertificateStatus.Void)
        ?? await _context.InsuranceCertificates.FirstOrDefaultAsync(item => item.ShipmentId == shipmentId && item.Status != OperationsCertificateStatus.Void, cancellationToken);

    public async Task<OperationsInsuranceCertificate?> FindLatestCertificateAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        await _context.InsuranceCertificates.Where(item => item.ShipmentId == shipmentId).OrderByDescending(item => item.CreatedUtc).FirstOrDefaultAsync(cancellationToken);
}
