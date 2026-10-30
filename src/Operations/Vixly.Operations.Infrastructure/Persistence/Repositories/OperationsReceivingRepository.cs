using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Infrastructure.Persistence.Repositories;

// =========================================================
// FILE: OperationsReceivingRepository.cs
//
// PURPOSE:
// Reads and writes receiving records: pre-alerts, shipments, receipts, photos, shelves and parcels (Slice 3B).
//
// RESPONSIBILITIES:
// - Implement IOperationsReceivingRepository over the Operations database
//
// IMPORTANT NOTES:
// - Every query runs inside the tenant filter (D-F1)
// - Records added earlier in the unit of work are found too, so one command sees its own changes
// =========================================================
internal sealed class OperationsReceivingRepository : IOperationsReceivingRepository
{
    private const int MaxSearchLength = 60;

    // Slice 5A (review): a page number past any real list is the last one asked, never an overflow.
    private const int MaxShipmentPage = 10_000;

    private readonly OperationsDbContext _context;

    public OperationsReceivingRepository(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public void Add(OperationsPreAlert preAlert) => _context.PreAlerts.Add(preAlert ?? throw new ArgumentNullException(nameof(preAlert)));

    public void Add(OperationsShipment shipment) => _context.Shipments.Add(shipment ?? throw new ArgumentNullException(nameof(shipment)));

    public void Add(OperationsWarehouseReceipt receipt) => _context.WarehouseReceipts.Add(receipt ?? throw new ArgumentNullException(nameof(receipt)));

    public void Add(OperationsParcelPhoto photo) => _context.ParcelPhotos.Add(photo ?? throw new ArgumentNullException(nameof(photo)));

    public void Add(OperationsStorageLocation location) => _context.StorageLocations.Add(location ?? throw new ArgumentNullException(nameof(location)));

    public void Add(OperationsInboundDelivery delivery) => _context.InboundDeliveries.Add(delivery ?? throw new ArgumentNullException(nameof(delivery)));

    public Task<OperationsInboundDelivery?> GetDeliveryAsync(Guid deliveryId, CancellationToken cancellationToken) =>
        _context.InboundDeliveries.FirstOrDefaultAsync(item => item.Id == deliveryId, cancellationToken);

    public Task<OperationsInboundDelivery?> FindDeliveryByOpenKeyAsync(string openKey, CancellationToken cancellationToken) =>
        _context.InboundDeliveries.AsNoTracking().FirstOrDefaultAsync(item => item.OpenKey == openKey, cancellationToken);

    public async Task<IReadOnlyList<(OperationsInboundDelivery Delivery, int Pieces)>> ListOpenDeliveriesAsync(IReadOnlyCollection<Guid> warehouseIds, CancellationToken cancellationToken)
    {
        List<Guid> warehouses = warehouseIds.ToList();
        var rows = await _context.InboundDeliveries.AsNoTracking()
            .Where(item => warehouses.Contains(item.WarehouseId) && item.Status == OperationsDeliveryStatus.Open)
            .OrderByDescending(item => item.CreatedUtc)
            .Take(50)
            .Select(item => new { Delivery = item, Pieces = _context.Parcels.Count(parcel => parcel.DeliveryId == item.Id) })
            .ToListAsync(cancellationToken);
        return rows.Select(row => (row.Delivery, row.Pieces)).ToList();
    }

    public async Task<(IReadOnlyList<OperationsWarehouseReceipt> Items, int TotalCount)> ListReceiptsAsync(IReadOnlyCollection<Guid> warehouseIds, int page, int pageSize, CancellationToken cancellationToken)
    {
        List<Guid> warehouses = warehouseIds.ToList();
        IQueryable<OperationsWarehouseReceipt> query = _context.WarehouseReceipts.AsNoTracking().Where(item => warehouses.Contains(item.WarehouseId));
        int total = await query.CountAsync(cancellationToken);
        List<OperationsWarehouseReceipt> items = await query.OrderByDescending(item => item.CreatedUtc).ThenBy(item => item.Id)
            .Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public Task<OperationsWarehouseReceipt?> GetReceiptAsync(Guid receiptId, CancellationToken cancellationToken) =>
        _context.WarehouseReceipts.AsNoTracking().FirstOrDefaultAsync(item => item.Id == receiptId, cancellationToken);

    public async Task<IReadOnlyList<OperationsParcel>> ListDeliveryParcelsAsync(Guid deliveryId, CancellationToken cancellationToken) =>
        await _context.Parcels.AsNoTracking().Where(item => item.DeliveryId == deliveryId).OrderBy(item => item.CreatedUtc).ThenBy(item => item.Number).ToListAsync(cancellationToken);

    public Task<OperationsParcel?> FindParcelByNumberAsync(string number, CancellationToken cancellationToken) =>
        _context.Parcels.AsNoTracking().FirstOrDefaultAsync(item => item.Number == number, cancellationToken);

    public async Task<IReadOnlyList<OperationsParcel>> ListShipmentParcelsAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        await _context.Parcels.AsNoTracking().Where(item => item.ShipmentId == shipmentId).OrderBy(item => item.CreatedUtc).Take(200).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsParcel>> ListShipmentParcelsForUpdateAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        // Loading attaches them; the context's own copies keep the unsaved changes, and parcels added in this unit of work are there too.
        await _context.Parcels.Where(item => item.ShipmentId == shipmentId).Take(200).LoadAsync(cancellationToken);
        return _context.Parcels.Local.Where(item => item.ShipmentId == shipmentId).OrderBy(item => item.CreatedUtc).ToList();
    }

    public Task<int> CountCustomerParcelsOnHoldAsync(Guid customerId, CancellationToken cancellationToken) =>
        _context.Parcels.AsNoTracking().CountAsync(item => item.CustomerId == customerId && item.HoldReasons != OperationsHoldReasons.None
            && (item.Milestone == null || item.Milestone < OperationsMilestone.DepartedOrigin), cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, int>> CountShipmentParcelsAsync(IReadOnlyCollection<Guid> shipmentIds, CancellationToken cancellationToken)
    {
        List<Guid> ids = shipmentIds.Distinct().Take(200).ToList();

        if (ids.Count == 0)
        {
            return new Dictionary<Guid, int>();
        }

        return await _context.Parcels.AsNoTracking()
            .Where(item => item.ShipmentId != null && ids.Contains(item.ShipmentId.Value) && (item.Milestone == null || item.Milestone < OperationsMilestone.Cancelled))
            .GroupBy(item => item.ShipmentId!.Value)
            .Select(group => new { group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Key, item => item.Count, cancellationToken);
    }

    public async Task<IReadOnlyList<OperationsPreAlert>> ListOpenPreAlertsByTrackingAsync(IReadOnlyCollection<string> trackingNumbers, CancellationToken cancellationToken)
    {
        List<string> numbers = trackingNumbers.Distinct(StringComparer.Ordinal).Take(10).ToList();
        return numbers.Count == 0
            ? Array.Empty<OperationsPreAlert>()
            : await _context.PreAlerts.Where(item => numbers.Contains(item.TrackingNumber) && item.Status == OperationsPreAlertStatus.Open).ToListAsync(cancellationToken);
    }

    public Task<OperationsPreAlert?> FindOpenPreAlertAsync(Guid customerId, string trackingNumber, CancellationToken cancellationToken) =>
        _context.PreAlerts.FirstOrDefaultAsync(item => item.CustomerId == customerId && item.TrackingNumber == trackingNumber && item.Status == OperationsPreAlertStatus.Open, cancellationToken);

    public Task<OperationsPreAlert?> GetPreAlertAsync(Guid preAlertId, CancellationToken cancellationToken) =>
        _context.PreAlerts.FirstOrDefaultAsync(item => item.Id == preAlertId, cancellationToken);

    public async Task<IReadOnlyList<OperationsPreAlert>> ListOpenPreAlertsAsync(Guid customerId, CancellationToken cancellationToken) =>
        await _context.PreAlerts.AsNoTracking().Where(item => item.CustomerId == customerId && item.Status == OperationsPreAlertStatus.Open)
            .OrderByDescending(item => item.CreatedUtc).Take(200).ToListAsync(cancellationToken);

    // The customer's open shipment without instructions at the warehouse, for a package that arrived before the customer said where it goes.
    public async Task<OperationsShipment?> FindOpenShipmentAsync(Guid customerId, Guid warehouseId, CancellationToken cancellationToken) =>
        _context.Shipments.Local.FirstOrDefault(item => item.CustomerId == customerId && item.OriginWarehouseId == warehouseId && item.Status == OperationsShipmentStatus.Open && item.DestinationCountryCode == null)
        ?? await _context.Shipments.FirstOrDefaultAsync(item => item.CustomerId == customerId && item.OriginWarehouseId == warehouseId && item.Status == OperationsShipmentStatus.Open
            && item.DestinationCountryCode == null, cancellationToken);

    public async Task<IReadOnlyList<OperationsParcel>> ListLabelsToPrintAsync(IReadOnlyCollection<Guid> warehouses, int take, CancellationToken cancellationToken)
    {
        if (warehouses.Count == 0)
        {
            return Array.Empty<OperationsParcel>();
        }

        List<Guid> shown = warehouses.ToList();
        const OperationsParcelFlags waiting = OperationsParcelFlags.AwaitingCheck | OperationsParcelFlags.InspectionPending;
        // Slice 5C: a final shipment still needs its labels.
        IQueryable<Guid> shipments = _context.Shipments.Where(item => (item.Status == OperationsShipmentStatus.Open || item.Status == OperationsShipmentStatus.Booked) && item.DestinationCountryCode != null).Select(item => item.Id);

        return await _context.Parcels.AsNoTracking()
            .Where(item => item.WarehouseId != null && shown.Contains(item.WarehouseId.Value) && item.ShippingLabelUtc == null
                && item.Milestone >= OperationsMilestone.ReceivedAtOrigin && item.Milestone < OperationsMilestone.DepartedOrigin
                && item.HoldReasons == OperationsHoldReasons.None && (item.Flags & waiting) == 0 && item.ShipmentId != null && shipments.Contains(item.ShipmentId.Value))
            .OrderBy(item => item.CreatedUtc).Take(take).ToListAsync(cancellationToken);
    }

    public Task<OperationsShipment?> FindShipmentByKeyAsync(Guid customerId, string requestKey, CancellationToken cancellationToken) =>
        _context.Shipments.AsNoTracking().FirstOrDefaultAsync(item => item.CustomerId == customerId && item.RequestKey == requestKey, cancellationToken);

    public Task<OperationsShipment?> GetShipmentAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        _context.Shipments.FirstOrDefaultAsync(item => item.Id == shipmentId, cancellationToken);

    public async Task<IReadOnlyList<OperationsShipment>> ListCustomerShipmentsAsync(Guid customerId, CancellationToken cancellationToken) =>
        await _context.Shipments.AsNoTracking().Where(item => item.CustomerId == customerId && item.Status != OperationsShipmentStatus.Cancelled)
            .OrderByDescending(item => item.CreatedUtc).Take(100).ToListAsync(cancellationToken);

    // Slice 4D: the incoming packages list.
    public async Task<(IReadOnlyList<OperationsIncomingRow> Items, int TotalCount)> ListIncomingAsync(OperationsIncomingFilter filter, int page, int pageSize,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.Warehouses.Count == 0)
        {
            return (Array.Empty<OperationsIncomingRow>(), 0);
        }

        IQueryable<OperationsPreAlert> query = Incoming(filter);
        DateOnly today = filter.Today;

        query = filter.View switch
        {
            OperationsIncomingView.Expected => query.Where(item => item.Status == OperationsPreAlertStatus.Open && (item.ExpectedDate == null || item.ExpectedDate >= today)),
            OperationsIncomingView.Overdue => query.Where(item => item.Status == OperationsPreAlertStatus.Open && item.ExpectedDate < today),
            OperationsIncomingView.Received => query.Where(item => item.Status == OperationsPreAlertStatus.Received),
            _ => query
        };

        int total = await query.CountAsync(cancellationToken);
        List<OperationsIncomingRow> rows = await (
            from preAlert in query.OrderByDescending(item => item.ExpectedDate).ThenByDescending(item => item.CreatedUtc).Skip((page - 1) * pageSize).Take(pageSize)
            join customer in _context.Customers.AsNoTracking() on preAlert.CustomerId equals customer.Id
            join received in _context.Parcels.AsNoTracking() on preAlert.ParcelId equals (Guid?)received.Id into parcels
            from parcel in parcels.DefaultIfEmpty()
            select new OperationsIncomingRow(preAlert.Id, preAlert.WarehouseId, customer.Code, customer.FirstName + " " + customer.LastName, preAlert.Carrier, preAlert.TrackingNumber,
                preAlert.Store, preAlert.Contents, preAlert.DeclaredValue, preAlert.CurrencyCode, preAlert.ExpectedDate, preAlert.Status, preAlert.ParcelId,
                parcel == null ? null : parcel.Number, preAlert.ShipmentId != null, preAlert.CreatedUtc))
            .ToListAsync(cancellationToken);

        return (rows, total);
    }

    // The counts beside the views: the same filters, every view.
    public async Task<OperationsIncomingCounts> CountIncomingAsync(OperationsIncomingFilter filter, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.Warehouses.Count == 0)
        {
            return new OperationsIncomingCounts(0, 0, 0);
        }

        IQueryable<OperationsPreAlert> query = Incoming(filter);
        DateOnly today = filter.Today;
        return new OperationsIncomingCounts(
            await query.CountAsync(item => item.Status == OperationsPreAlertStatus.Open && (item.ExpectedDate == null || item.ExpectedDate >= today), cancellationToken),
            await query.CountAsync(item => item.Status == OperationsPreAlertStatus.Open && item.ExpectedDate < today, cancellationToken),
            await query.CountAsync(item => item.Status == OperationsPreAlertStatus.Received, cancellationToken));
    }

    // The pre-alerts of the warehouses, with the expected days, carrier and search; any view.
    private IQueryable<OperationsPreAlert> Incoming(OperationsIncomingFilter filter)
    {
        List<Guid> warehouses = filter.Warehouses.ToList();
        IQueryable<OperationsPreAlert> query = _context.PreAlerts.AsNoTracking()
            .Where(item => item.WarehouseId != null && warehouses.Contains(item.WarehouseId.Value) && item.Status != OperationsPreAlertStatus.Cancelled);

        if (filter.From is { } from)
        {
            query = query.Where(item => item.ExpectedDate >= from);
        }

        if (filter.To is { } to)
        {
            query = query.Where(item => item.ExpectedDate <= to);
        }

        if (filter.Carrier is { } carrier)
        {
            query = query.Where(item => item.Carrier == carrier);
        }

        string text = new((filter.Search ?? string.Empty).Where(character => !char.IsWhiteSpace(character) && character != '-').Take(MaxSearchLength).ToArray());
        text = text.ToUpperInvariant();

        // A carrier tracking number (or its end) or a customer code.
        if (text.Length > 0)
        {
            IQueryable<Guid> customers = _context.Customers.Where(customer => customer.Code == text).Select(customer => customer.Id);
            query = query.Where(item => item.TrackingNumber == text || (text.Length >= 6 && item.TrackingNumber.EndsWith(text)) || customers.Contains(item.CustomerId));
        }

        return query;
    }

    // Slice 5A: the shipments of the warehouses (by origin), newest first, with what is in them.
    public async Task<(IReadOnlyList<Vixly.Operations.Application.Shipments.OperationsShipmentRow> Items, int TotalCount)> ListShipmentsAsync(
        Vixly.Operations.Application.Shipments.OperationsShipmentFilter filter, int page, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.Warehouses.Count == 0)
        {
            return (Array.Empty<Vixly.Operations.Application.Shipments.OperationsShipmentRow>(), 0);
        }

        IQueryable<OperationsShipment> query = filter.View switch
        {
            Vixly.Operations.Application.Shipments.OperationsShipmentView.ToSend => Shipments(filter).Where(item => (item.Status == OperationsShipmentStatus.Open || item.Status == OperationsShipmentStatus.Booked) && item.DestinationCountryCode != null),
            Vixly.Operations.Application.Shipments.OperationsShipmentView.Waiting => Shipments(filter).Where(item => item.Status == OperationsShipmentStatus.Open && item.DestinationCountryCode == null),
            Vixly.Operations.Application.Shipments.OperationsShipmentView.Cancelled => Shipments(filter).Where(item => item.Status == OperationsShipmentStatus.Cancelled),
            _ => Shipments(filter)
        };

        int total = await query.CountAsync(cancellationToken);
        List<Vixly.Operations.Application.Shipments.OperationsShipmentRow> rows = await (
            from shipment in query.OrderByDescending(item => item.CreatedUtc).ThenByDescending(item => item.Id).Skip((Math.Clamp(page, 1, MaxShipmentPage) - 1) * pageSize).Take(pageSize)
            join customer in _context.Customers.AsNoTracking() on shipment.CustomerId equals customer.Id
            select new Vixly.Operations.Application.Shipments.OperationsShipmentRow(
                shipment.Id, shipment.Number, shipment.OriginWarehouseId, customer.Code, shipment.Status, shipment.DestinationCountryCode,
                shipment.DeliveryAddress == null ? null : shipment.DeliveryAddress.City, shipment.RecipientName, shipment.ServiceCode,
                _context.PreAlerts.Count(preAlert => preAlert.ShipmentId == shipment.Id && preAlert.Status == OperationsPreAlertStatus.Open),
                _context.Parcels.Count(parcel => parcel.ShipmentId == shipment.Id),
                _context.Parcels.Count(parcel => parcel.ShipmentId == shipment.Id && parcel.ShippingLabelUtc != null),
                shipment.CreatedUtc))
            .ToListAsync(cancellationToken);

        return (rows, total);
    }

    public async Task<Vixly.Operations.Application.Shipments.OperationsShipmentCounts> CountShipmentsAsync(Vixly.Operations.Application.Shipments.OperationsShipmentFilter filter,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.Warehouses.Count == 0)
        {
            return new Vixly.Operations.Application.Shipments.OperationsShipmentCounts(0, 0, 0);
        }

        IQueryable<OperationsShipment> query = Shipments(filter);
        return new Vixly.Operations.Application.Shipments.OperationsShipmentCounts(
            await query.CountAsync(item => (item.Status == OperationsShipmentStatus.Open || item.Status == OperationsShipmentStatus.Booked) && item.DestinationCountryCode != null, cancellationToken),
            await query.CountAsync(item => item.Status == OperationsShipmentStatus.Open && item.DestinationCountryCode == null, cancellationToken),
            await query.CountAsync(item => item.Status == OperationsShipmentStatus.Cancelled, cancellationToken));
    }

    // The shipments from the warehouses, with the service, country and search (a shipment number, a customer code or the recipient's name); any view.
    private IQueryable<OperationsShipment> Shipments(Vixly.Operations.Application.Shipments.OperationsShipmentFilter filter)
    {
        List<Guid> warehouses = filter.Warehouses.ToList();
        IQueryable<OperationsShipment> query = _context.Shipments.AsNoTracking().Where(item => warehouses.Contains(item.OriginWarehouseId));

        if (filter.ServiceCode is { } service)
        {
            query = query.Where(item => item.ServiceCode == service);
        }

        if (filter.CountryCode is { } country)
        {
            query = query.Where(item => item.DestinationCountryCode == country);
        }

        string text = (filter.Search ?? string.Empty).Trim();
        text = text.Length > MaxSearchLength ? text[..MaxSearchLength] : text;

        if (text.Length > 0)
        {
            string upper = text.ToUpperInvariant();
            IQueryable<Guid> customers = _context.Customers.Where(customer => customer.Code == upper).Select(customer => customer.Id);
            query = query.Where(item => item.Number == upper || customers.Contains(item.CustomerId) || (item.RecipientName != null && item.RecipientName.Contains(text)));
        }

        return query;
    }

    public async Task<IReadOnlyList<OperationsPreAlert>> ListShipmentPreAlertsAsync(Guid shipmentId, CancellationToken cancellationToken) =>
        await _context.PreAlerts.Where(item => item.ShipmentId == shipmentId && item.Status != OperationsPreAlertStatus.Cancelled)
            .OrderBy(item => item.CreatedUtc).Take(200).ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsPreAlert>> ListShipmentPreAlertsForUpdateAsync(Guid shipmentId, CancellationToken cancellationToken)
    {
        await _context.PreAlerts.Where(item => item.ShipmentId == shipmentId).Take(200).LoadAsync(cancellationToken);
        return _context.PreAlerts.Local.Where(item => item.ShipmentId == shipmentId).OrderBy(item => item.CreatedUtc).ToList();
    }

    public Task<OperationsWarehouseReceipt?> FindReceiptAsync(Guid parcelId, CancellationToken cancellationToken) =>
        (from parcel in _context.Parcels.AsNoTracking()
         join receipt in _context.WarehouseReceipts.AsNoTracking() on parcel.DeliveryId equals receipt.DeliveryId
         where parcel.Id == parcelId
         select receipt).FirstOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<OperationsParcelPhoto>> ListPhotosAsync(Guid parcelId, CancellationToken cancellationToken) =>
        await _context.ParcelPhotos.AsNoTracking().Where(item => item.ParcelId == parcelId).ToListAsync(cancellationToken);

    public Task<OperationsParcelPhoto?> GetPhotoAsync(Guid photoId, CancellationToken cancellationToken) =>
        _context.ParcelPhotos.AsNoTracking().FirstOrDefaultAsync(item => item.Id == photoId, cancellationToken);

    public async Task<IReadOnlyList<OperationsStorageLocation>> ListLocationsAsync(Guid warehouseId, CancellationToken cancellationToken) =>
        await _context.StorageLocations.Where(item => item.WarehouseId == warehouseId).OrderBy(item => item.Code).ToListAsync(cancellationToken);

    public Task<OperationsStorageLocation?> GetLocationAsync(Guid locationId, CancellationToken cancellationToken) =>
        _context.StorageLocations.FirstOrDefaultAsync(item => item.Id == locationId, cancellationToken);

    public async Task<IReadOnlyList<OperationsStorageLocation>> ListLocationsByIdAsync(IReadOnlyCollection<Guid> locationIds, CancellationToken cancellationToken) =>
        locationIds is { Count: > 0 }
            ? await _context.StorageLocations.AsNoTracking().Where(item => locationIds.Contains(item.Id)).ToListAsync(cancellationToken)
            : Array.Empty<OperationsStorageLocation>();

    public Task<bool> LocationCodeTakenAsync(Guid warehouseId, string code, Guid? exceptId, CancellationToken cancellationToken) =>
        _context.StorageLocations.AnyAsync(item => item.WarehouseId == warehouseId && item.Code == code && item.Id != exceptId, cancellationToken);

    // Still at the origin: not yet departed, and not cancelled, returned or disposed of (the origin key is cleared then).
    // Every reading of a barcode at every given warehouse, in one query on the unique origin key.
    public Task<OperationsParcel?> FindActiveParcelAsync(IReadOnlyCollection<Guid> warehouseIds, IReadOnlyCollection<string> carrierTrackingNumbers, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(warehouseIds);
        ArgumentNullException.ThrowIfNull(carrierTrackingNumbers);
        List<string> keys = warehouseIds.SelectMany(warehouse => carrierTrackingNumbers.Select(tracking => OperationsParcel.OriginKeyOf(warehouse, tracking))).Take(200).ToList();

        return keys.Count == 0
            ? Task.FromResult<OperationsParcel?>(null)
            : _context.Parcels.AsNoTracking().FirstOrDefaultAsync(item => item.OriginKey != null && keys.Contains(item.OriginKey), cancellationToken);
    }

    public Task<OperationsParcel?> FindCustomerParcelByTrackingAsync(Guid customerId, string carrierTrackingNumber, CancellationToken cancellationToken) =>
        _context.Parcels.FirstOrDefaultAsync(item => item.CustomerId == customerId && item.CarrierTrackingNumber == carrierTrackingNumber
            && (item.Milestone == null || item.Milestone < OperationsMilestone.DepartedOrigin), cancellationToken);

    public async Task<(IReadOnlyList<OperationsParcel> Items, int TotalCount)> ListParcelsAsync(OperationsParcelFilter filter, int page, int pageSize, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(filter);

        if (filter.Warehouses.Count == 0)
        {
            return (Array.Empty<OperationsParcel>(), 0);
        }

        List<Guid> warehouses = filter.Warehouses.ToList();
        IQueryable<OperationsParcel> query = _context.Parcels.AsNoTracking().Where(item => item.WarehouseId != null && warehouses.Contains(item.WarehouseId.Value));
        string text = new((filter.Search ?? string.Empty).Where(character => !char.IsWhiteSpace(character) && character != '-').Take(MaxSearchLength).ToArray());
        text = text.ToUpperInvariant();

        // A parcel number, a carrier tracking number (or its end, as printed small on some labels) or a customer code.
        if (text.Length > 0)
        {
            IQueryable<Guid> customers = _context.Customers.Where(customer => customer.Code == text).Select(customer => customer.Id);
            // Slice 4D step 3: or what the label of an undeclared package shows (the mailbox code, a name or sender written on it).
            string words = (filter.Search ?? string.Empty).Trim() is { Length: > 100 } longer ? longer[..100] : (filter.Search ?? string.Empty).Trim();
            query = query.Where(item => item.Number == text || item.CarrierTrackingNumber == text || (text.Length >= 6 && item.CarrierTrackingNumber!.EndsWith(text))
                || (item.CustomerId != null && customers.Contains(item.CustomerId.Value)) || item.LabelCode == text
                || (words.Length >= 3 && item.CustomerId == null && (item.LabelName!.Contains(words) || item.LabelSender!.Contains(words))));
        }

        if (filter.UnidentifiedOnly)
        {
            query = query.Where(item => item.CustomerId == null);
        }

        if (filter.HeldOnly)
        {
            query = query.Where(item => item.HoldReasons != OperationsHoldReasons.None);
        }

        int total = await query.CountAsync(cancellationToken);
        List<OperationsParcel> items = await query.OrderByDescending(item => item.CreatedUtc).ThenBy(item => item.Id)
            .Skip((Math.Max(page, 1) - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }

    public async Task<IReadOnlyList<OperationsParcel>> ListCustomerParcelsAsync(Guid customerId, CancellationToken cancellationToken) =>
        await _context.Parcels.AsNoTracking().Where(item => item.CustomerId == customerId).OrderByDescending(item => item.CreatedUtc).Take(200).ToListAsync(cancellationToken);
}
