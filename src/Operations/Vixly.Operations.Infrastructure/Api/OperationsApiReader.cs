using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Infrastructure.Persistence;

namespace Vixly.Operations.Infrastructure.Api;

// =========================================================
// FILE: OperationsApiReader.cs
//
// PURPOSE:
// Reads the company’s data as its API and webhooks show it (Slice 10).
//
// RESPONSIBILITIES:
// - A page after a place, in order of creation then id; one record by id
// - Turn each record into its API view
//
// IMPORTANT NOTES:
// - Tenant-filtered (D-F1), not tracked; the order and the place are compared in SQL, so a page never skips or repeats a row
// =========================================================
internal sealed class OperationsApiReader : IOperationsApiReader
{
    private readonly OperationsDbContext _context;

    public OperationsApiReader(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<IReadOnlyList<OperationsApiRow>> ListAsync(OperationsApiResource resource, OperationsApiQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        int take = Math.Clamp(query.Limit, 1, 101);
        OperationsTrackingSubject? subject = query.SubjectResource switch
        {
            OperationsApiResource.Parcels => OperationsTrackingSubject.Parcel,
            OperationsApiResource.Shipments => OperationsTrackingSubject.Shipment,
            _ => null
        };

        return resource switch
        {
            OperationsApiResource.Customers => (await Page(_context.Customers.AsNoTracking().Where(item => query.CustomerId == null || item.Id == query.CustomerId), query, take)
                .ToListAsync(cancellationToken)).Select(item => new OperationsApiRow(item.CreatedUtc, item.Id, Customer(item))).ToList(),
            OperationsApiResource.Shipments => (await Page(_context.Shipments.AsNoTracking()
                    .Where(item => (query.CustomerId == null || item.CustomerId == query.CustomerId)), query, take)
                .ToListAsync(cancellationToken)).Select(item => new OperationsApiRow(item.CreatedUtc, item.Id, Shipment(item))).ToList(),
            OperationsApiResource.Parcels => (await Page(_context.Parcels.AsNoTracking()
                    .Where(item => (query.CustomerId == null || item.CustomerId == query.CustomerId) && (query.ShipmentId == null || item.ShipmentId == query.ShipmentId)), query, take)
                .ToListAsync(cancellationToken)).Select(item => new OperationsApiRow(item.CreatedUtc, item.Id, Parcel(item))).ToList(),
            OperationsApiResource.Tracking => (await Page(_context.TrackingEvents.AsNoTracking()
                    .Where(item => (subject == null ? item.SubjectKind == OperationsTrackingSubject.Parcel || item.SubjectKind == OperationsTrackingSubject.Shipment
                        : item.SubjectKind == subject) && (query.SubjectId == null || item.SubjectId == query.SubjectId)), query, take)
                .ToListAsync(cancellationToken)).Select(item => new OperationsApiRow(item.CreatedUtc, item.Id, Tracking(item))).ToList(),
            OperationsApiResource.Invoices => (await Page(_context.Invoices.AsNoTracking()
                    .Where(item => (query.CustomerId == null || item.CustomerId == query.CustomerId) && (query.ShipmentId == null || item.ShipmentId == query.ShipmentId)), query, take)
                .ToListAsync(cancellationToken)).Select(item => new OperationsApiRow(item.CreatedUtc, item.Id, Invoice(item))).ToList(),
            OperationsApiResource.Payments => (await Page(_context.Payments.AsNoTracking().Where(item => query.CustomerId == null || item.CustomerId == query.CustomerId), query, take)
                .ToListAsync(cancellationToken)).Select(item => new OperationsApiRow(item.CreatedUtc, item.Id, Payment(item))).ToList(),
            _ => Array.Empty<OperationsApiRow>()
        };
    }

    public async Task<object?> GetAsync(OperationsApiResource resource, Guid id, CancellationToken cancellationToken) => resource switch
    {
        OperationsApiResource.Customers => await _context.Customers.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken) is { } customer ? Customer(customer) : null,
        OperationsApiResource.Shipments => await _context.Shipments.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id,
            cancellationToken) is { } shipment ? Shipment(shipment) : null,
        OperationsApiResource.Parcels => await _context.Parcels.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken) is { } parcel ? Parcel(parcel) : null,
        OperationsApiResource.Invoices => await _context.Invoices.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken) is { } invoice ? Invoice(invoice) : null,
        OperationsApiResource.Payments => await _context.Payments.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id, cancellationToken) is { } payment ? Payment(payment) : null,
        _ => null
    };

    public async Task<IReadOnlyDictionary<Guid, object>> GetManyAsync(OperationsApiResource resource, IReadOnlyCollection<Guid> ids, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(ids);
        List<Guid> wanted = ids.Take(500).ToList();

        return resource switch
        {
            OperationsApiResource.Customers => (await _context.Customers.AsNoTracking().Where(item => wanted.Contains(item.Id)).ToListAsync(cancellationToken))
                .ToDictionary(item => item.Id, item => (object)Customer(item)),
            OperationsApiResource.Tracking => (await _context.TrackingEvents.AsNoTracking().Where(item => wanted.Contains(item.Id)).ToListAsync(cancellationToken))
                .ToDictionary(item => item.Id, item => (object)Tracking(item)),
            OperationsApiResource.Invoices => (await _context.Invoices.AsNoTracking().Where(item => wanted.Contains(item.Id)).ToListAsync(cancellationToken))
                .ToDictionary(item => item.Id, item => (object)Invoice(item)),
            OperationsApiResource.Payments => (await _context.Payments.AsNoTracking().Where(item => wanted.Contains(item.Id)).ToListAsync(cancellationToken))
                .ToDictionary(item => item.Id, item => (object)Payment(item)),
            OperationsApiResource.Shipments => (await _context.Shipments.AsNoTracking().Where(item => wanted.Contains(item.Id)).ToListAsync(cancellationToken))
                .ToDictionary(item => item.Id, item => (object)Shipment(item)),
            OperationsApiResource.Parcels => (await _context.Parcels.AsNoTracking().Where(item => wanted.Contains(item.Id)).ToListAsync(cancellationToken))
                .ToDictionary(item => item.Id, item => (object)Parcel(item)),
            _ => new Dictionary<Guid, object>()
        };
    }

    // After the place (time, then id), before the settling edge, oldest first.
    private static IQueryable<T> Page<T>(IQueryable<T> rows, OperationsApiQuery query, int take)
        where T : Vixly.Operations.Domain.Common.OperationsEntity
    {
        DateTimeOffset afterUtc = query.AfterUtc;
        Guid afterId = query.AfterId;
        IQueryable<T> page = rows.Where(item => item.CreatedUtc > afterUtc || (item.CreatedUtc == afterUtc && item.Id > afterId));

        if (query.BeforeUtc is { } before)
        {
            page = page.Where(item => item.CreatedUtc < before);
        }

        return page.OrderBy(item => item.CreatedUtc).ThenBy(item => item.Id).Take(take);
    }

    private static OperationsApiCustomer Customer(OperationsCustomer item) =>
        new(item.Id, item.Code, item.FirstName, item.LastName, item.CompanyName, item.Email.Value, item.Phone, item.PreferredLanguage.Value, item.Status.ToString(), item.CreatedUtc);

    private static OperationsApiShipment Shipment(OperationsShipment item) =>
        new(item.Id, item.Number, item.CustomerId, item.Status.ToString(), item.OriginWarehouseId, item.DestinationWarehouseId, item.DestinationBranchId, item.DestinationCountryCode,
            item.ServiceCode, item.RecipientName, item.ParcelCount, item.BookedTotal, item.BookedCurrencyCode, item.BookedUtc, item.PickupReadyUtc, item.PickedUpUtc, item.DeliveredUtc,
            item.CreatedUtc);

    private static OperationsApiParcel Parcel(OperationsParcel item) =>
        new(item.Id, item.Number, item.CustomerId, item.ShipmentId, item.WarehouseId, item.Carrier, item.CarrierTrackingNumber, item.WeightKg, item.LengthCm, item.WidthCm,
            item.HeightCm, item.DeclaredContents, item.DeclaredValue, item.CurrencyCode, item.Milestone?.ToString(), item.LastEventUtc, item.CreatedUtc);

    private static OperationsApiTrackingEvent Tracking(OperationsTrackingEvent item) =>
        new(item.Id, item.SubjectKind.ToString().ToLowerInvariant(), item.SubjectId, item.Code, item.Arguments(), item.OccurredUtc, item.TimeZone, item.LocationCode,
            item.Visibility.ToString().ToLowerInvariant(), item.VoidsEventId, item.CreatedUtc);

    private static OperationsApiInvoice Invoice(Vixly.Operations.Domain.Billing.OperationsInvoice item) =>
        new(item.Id, item.Number, item.Kind.ToString(), item.CustomerId, item.ShipmentId, item.CurrencyCode, item.IssuedOn, item.DueOn, item.Status.ToString(),
            item.PaymentState.ToString(), item.Subtotal, item.TaxTotal, item.Total, item.CreditedTotal, item.PaidTotal, item.Owed, item.CreatedUtc);

    private static OperationsApiPayment Payment(OperationsPayment item) =>
        new(item.Id, item.Number, item.InvoiceId, item.CustomerId, item.CurrencyCode, item.Total, item.Pending, item.Tenders.Select(tender => tender.Method.ToString()).Distinct().ToList(),
            item.TakenUtc, item.CreatedUtc);
}
