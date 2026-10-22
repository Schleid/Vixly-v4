using Vixly.Operations.Domain.Identity;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsAccountTokenRepository.cs
//
// PURPOSE:
// Emailed account links of the current tenant.
//
// RESPONSIBILITIES:
// - Add and find by token hash
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1)
// =========================================================
public interface IOperationsAccountTokenRepository
{
    void Add(OperationsAccountToken token);

    Task<OperationsAccountToken?> FindByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);
}
