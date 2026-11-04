using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformShipmentUsageRepository.cs
//
// PURPOSE:
// Loads and stores the monthly shipment counts Operations reports (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Find a company's month
// - List the latest month of several companies for Control
// - Add a month
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure
// =========================================================
public interface IPlatformShipmentUsageRepository
{
    Task<PlatformTenantShipmentUsage?> GetAsync(Guid platformTenantId, string month, CancellationToken cancellationToken);

    Task<IReadOnlyList<PlatformTenantShipmentUsage>> ListLatestAsync(IReadOnlyCollection<Guid> platformTenantIds, CancellationToken cancellationToken);

    void Add(PlatformTenantShipmentUsage usage);
}
