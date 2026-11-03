namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsShipmentCounter.cs
//
// PURPOSE:
// Counts the shipments a company created in a period (shipment cap, owner 2026-10-29).
//
// RESPONSIBILITIES:
// - Count the company's shipments created between two moments, cancelled ones left out
// - List the companies that created shipments since a moment (for the hourly report)
//
// IMPORTANT NOTES:
// - Implemented in Infrastructure; the count follows the current tenant scope
// =========================================================
public interface IOperationsShipmentCounter
{
    Task<int> CountCreatedAsync(DateTimeOffset fromUtc, DateTimeOffset toUtc, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListTenantsCreatingSinceAsync(DateTimeOffset sinceUtc, CancellationToken cancellationToken);
}
