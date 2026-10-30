using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: IOperationsPickupRepository.cs
//
// PURPOSE:
// Reads and writes what the pickup counter needs (Slice 7 step 2).
//
// RESPONSIBILITIES:
// - The shipments ready at a warehouse, their invoices of every kind, the releases
// - The parcels waiting for a pickup reminder
//
// IMPORTANT NOTES:
// - Tenant-scoped by the query filter, except the list of companies with parcels to remind (system scope)
// =========================================================
public interface IOperationsPickupRepository
{
    void Add(OperationsPickupRelease release);

    // Counts a try of the shipment's pickup code in one statement, unless five were counted already; the shipment is read again after (review).
    Task<bool> CountCodeTryAsync(OperationsShipment shipment, CancellationToken cancellationToken);

    Task<OperationsPickupRelease?> FindByKeyAsync(Guid requestKey, CancellationToken cancellationToken);

    Task<IReadOnlyList<OperationsPickupRelease>> ListReleasesAsync(Guid shipmentId, CancellationToken cancellationToken);

    // Shipments with parcels ready for pickup at this warehouse, oldest ready first; the text narrows by number, recipient, person named or phone.
    Task<IReadOnlyList<(OperationsShipment Shipment, int Ready, DateTimeOffset ReadySince)>> ListReadyAsync(Guid warehouseId, string? text, int take, CancellationToken cancellationToken);

    // Every invoice of these shipments (the shipment's and its destination invoice), not voided.
    Task<IReadOnlyList<Vixly.Operations.Domain.Billing.OperationsInvoice>> ListInvoicesAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken);

    // Shipments waiting at their pickup counter whose next reminder is due, oldest first; tracked (review: due ones only, kept on the shipment).
    Task<IReadOnlyList<OperationsShipment>> ListRemindableAsync(DateTimeOffset utcNow, int take, CancellationToken cancellationToken);

    Task<IReadOnlyList<Guid>> ListTenantsWithRemindableAsync(DateTimeOffset utcNow, CancellationToken cancellationToken);
}
