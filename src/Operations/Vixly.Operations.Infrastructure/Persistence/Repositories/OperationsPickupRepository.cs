using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;
using OperationsInvoice = Vixly.Operations.Domain.Billing.OperationsInvoice;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Pickup;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsPickupRepository.cs
//
// PURPOSE:
// EF Core access for the pickup counter (Slice 7 step 2).
//
// RESPONSIBILITIES:
// - Shipments ready at a warehouse, their invoices, releases, parcels to remind
//
// IMPORTANT NOTES:
// - Tenant-scoped by the query filter; the list of companies runs in the system scope
// =========================================================
internal sealed class OperationsPickupRepository : IOperationsPickupRepository
{
    private readonly OperationsDbContext _context;

    public OperationsPickupRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsPickupRelease release) => _context.PickupReleases.Add(release ?? throw new ArgumentNullException(nameof(release)));

    public async Task<bool> CountCodeTryAsync(OperationsShipment shipment, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipment);
        int counted = await _context.Shipments.Where(item => item.Id == shipment.Id && item.PickupCodeFailures < OperationsShipment.PickupCodeTries)
            .ExecuteUpdateAsync(set => set.SetProperty(item => item.PickupCodeFailures, item => item.PickupCodeFailures + 1), cancellationToken);
        await _context.Entry(shipment).ReloadAsync(cancellationToken);
        return counted == 1;
    }

    public Task<OperationsPickupRelease?> FindByKeyAsync(Guid requestKey, CancellationToken cancellationToken) =>
        _context.PickupReleases.AsNoTracking().FirstOrDefaultAsync(item => item.RequestKey == requestKey, cancellationToken);

    public async Task<IReadOnlyList<OperationsPickupRelease>> ListReleasesAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        await _context.PickupReleases.AsNoTracking().Where(item => item.ShipmentId == shipmentId).OrderBy(item => item.CreatedUtc).Take(100).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<(OperationsShipment Shipment, int Ready, DateTimeOffset ReadySince)>> ListReadyAsync(Guid warehouseId, string? text, int take,
        CancellationToken cancellationToken)
    {
        var groups = _context.Parcels.AsNoTracking()
            .Where(parcel => parcel.WarehouseId == warehouseId && parcel.Milestone == OperationsMilestone.ReadyForPickup && parcel.ShipmentId != null
                && (parcel.Flags & OperationsParcelFlags.CustomsHeld) == 0)
            .GroupBy(parcel => parcel.ShipmentId!.Value)
            .Select(group => new { ShipmentId = group.Key, Ready = group.Count(), Since = group.Min(parcel => parcel.ReadyUtc ?? parcel.LastEventUtc ?? parcel.CreatedUtc) });
        IQueryable<OperationsShipment> shipments = _context.Shipments.AsNoTracking().Where(shipment => shipment.Status == OperationsShipmentStatus.Booked);

        if (text is not null)
        {
            string digits = new(text.Where(char.IsAsciiDigit).ToArray());

            // The text as typed, never a pattern (review).
            string like = "%" + text.Replace("[", "[[]", StringComparison.Ordinal).Replace("%", "[%]", StringComparison.Ordinal).Replace("_", "[_]", StringComparison.Ordinal) + "%";
            string number = text.ToUpperInvariant();
            shipments = shipments.Where(shipment => shipment.Number == number || (shipment.RecipientName != null && EF.Functions.Like(shipment.RecipientName, like))
                || (shipment.CollectorName != null && EF.Functions.Like(shipment.CollectorName, like))
                || (digits.Length >= 7 && ((shipment.RecipientPhone != null && shipment.RecipientPhone.EndsWith(digits)) || (shipment.CollectorPhone != null && shipment.CollectorPhone.EndsWith(digits)))));
        }

        var rows = await (from shipment in shipments
                          join ready in groups on shipment.Id equals ready.ShipmentId
                          orderby ready.Since
                          select new { shipment, ready.Ready, ready.Since }).Take(take).ToListAsync(cancellationToken);
        return rows.Select(row => (row.shipment, row.Ready, row.Since)).ToList();
    }

    public async Task<IReadOnlyList<OperationsInvoice>> ListInvoicesAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(shipmentIds);
        List<OperationsInvoice> found = new();

        foreach (Guid[] slice in shipmentIds.Distinct().Chunk(1000))
        {
            found.AddRange(await _context.Invoices.AsNoTracking().Where(invoice => invoice.ShipmentId != null && slice.Contains(invoice.ShipmentId.Value)
                && (invoice.Kind == OperationsInvoiceKind.Shipment || invoice.Kind == OperationsInvoiceKind.Destination) && invoice.Status != OperationsInvoiceStatus.Voided)
                .ToListAsync(cancellationToken));
        }

        return found;
    }

    public async Task<IReadOnlyList<OperationsShipment>> ListRemindableAsync(DateTimeOffset utcNow, int take, CancellationToken cancellationToken) =>
        await Remindable(utcNow).OrderBy(shipment => shipment.PickupReadyUtc).Take(take).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Guid>> ListTenantsWithRemindableAsync(DateTimeOffset utcNow, CancellationToken cancellationToken) =>
        await Remindable(utcNow).AsNoTracking().Select(shipment => shipment.TenantId).Distinct().Take(10_000).ToListAsync(cancellationToken);

    // Waiting at its pickup counter, not collected, with its next step due (7, 14, 21, 25 days).
    private IQueryable<OperationsShipment> Remindable(DateTimeOffset utcNow)
    {
        IReadOnlyList<int> days = OperationsHoldLimits.ReminderDays;
        DateTimeOffset first = utcNow.AddDays(-days[0]);
        DateTimeOffset second = utcNow.AddDays(-days[1]);
        DateTimeOffset third = utcNow.AddDays(-days[2]);
        DateTimeOffset fourth = utcNow.AddDays(-days[3]);
        // Only while a parcel waits ready and the company charges storage (review).
        return _context.Shipments.Where(shipment => shipment.PickupReadyUtc != null && shipment.PickedUpUtc == null
            && _context.Parcels.Any(parcel => parcel.ShipmentId == shipment.Id && parcel.Milestone == OperationsMilestone.ReadyForPickup)
            && _context.TenantSettings.Any(settings => settings.TenantId == shipment.TenantId && settings.DestinationStorage)
            && ((shipment.PickupRemindersSent == 0 && shipment.PickupReadyUtc <= first) || (shipment.PickupRemindersSent == 1 && shipment.PickupReadyUtc <= second)
                || (shipment.PickupRemindersSent == 2 && shipment.PickupReadyUtc <= third) || (shipment.PickupRemindersSent == 3 && shipment.PickupReadyUtc <= fourth)));
    }
}
