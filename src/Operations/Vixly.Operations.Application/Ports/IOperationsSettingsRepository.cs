using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsSettingsRepository.cs
//
// PURPOSE:
// Stores the tenant's settings row (Slice 1E).
//
// RESPONSIBILITIES:
// - Find the row of the current tenant, or add it
//
// IMPORTANT NOTES:
// - Tenant-filtered like every tenant table (D-F1)
// =========================================================
public interface IOperationsSettingsRepository
{
    void Add(OperationsTenantSettings settings);

    Task<OperationsTenantSettings?> FindAsync(CancellationToken cancellationToken);
}
