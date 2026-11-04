using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsShippingServiceRepository.cs
//
// PURPOSE:
// Stores the tenant's shipping-service setups (Slice 1E).
//
// RESPONSIBILITIES:
// - List and add setups of the current tenant
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1)
// =========================================================
public interface IOperationsShippingServiceRepository
{
    void Add(OperationsShippingServiceSetup setup);

    Task<IReadOnlyList<OperationsShippingServiceSetup>> ListAsync(CancellationToken cancellationToken);
}
