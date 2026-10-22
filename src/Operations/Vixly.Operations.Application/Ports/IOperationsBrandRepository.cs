using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsBrandRepository.cs
//
// PURPOSE:
// The brand of the current tenant.
//
// RESPONSIBILITIES:
// - Find and add
//
// IMPORTANT NOTES:
// - One per tenant
// =========================================================
public interface IOperationsBrandRepository
{
    Task<OperationsTenantBrand?> FindAsync(CancellationToken cancellationToken);

    void Add(OperationsTenantBrand brand);
}
