using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Payables;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Infrastructure.Persistence;
using static Vixly.Operations.Infrastructure.Reports.OperationsReportReader;

namespace Vixly.Operations.Infrastructure.Reports;

// =========================================================
// FILE: OperationsReportReaderMoney.cs
//
// PURPOSE:
// The finance, customer, carrier and warehouse reports (Slice 11).
//
// RESPONSIBILITIES:
// - Sales by period, service, route, customer or warehouse; receivables and payables by age; payments by method; taxes
// - Customers: sign-ups and active ones, the top 50, the inactive; carriers: estimated and billed cost, transit times; stock age; supplies value
//
// IMPORTANT NOTES:
// - Sales count issued invoices (not voided) on their issue day (already a local date)
// - Ageing uses balances as they are now, aged at the end date
// =========================================================
internal sealed class OperationsReportReaderMoney
{
    private readonly OperationsDbContext _context;

    public OperationsReportReaderMoney(OperationsDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    internal async Task<OperationsReportTable> SalesAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        List<Guid> places = query.Places.Select(place => place.Id).ToList();
        var invoices = await _context.Invoices.AsNoTracking()
            .Where(item => item.Status == OperationsInvoiceStatus.Issued && item.IssuedOn >= query.From && item.IssuedOn <= query.To && places.Contains(item.WarehouseId))
            .GroupBy(item => new { item.IssuedOn, item.ServiceCode, item.Route, item.CustomerId, item.WarehouseId })
            .Select(group => new { group.Key, Count = group.Count(), Subtotal = group.Sum(item => item.Subtotal), Tax = group.Sum(item => item.TaxTotal), Total = group.Sum(item => item.Total) })
            .ToListAsync(cancellationToken);

        if (query.Dimension == OperationsReportDimension.Period)
        {
            return OverTime(query, 4, add =>
            {
                foreach (var item in invoices)
                {
                    add(item.Key.IssuedOn, 0, item.Count);
                    add(item.Key.IssuedOn, 1, item.Subtotal);
                    add(item.Key.IssuedOn, 2, item.Tax);
                    add(item.Key.IssuedOn, 3, item.Total);
                }
            }) is var table ? table with { Rows = table.Rows.Select(row => row with { Values = Hide(query, row.Values.ToArray()) }).ToList(), Totals = Hide(query, table.Totals.ToArray()) } : null!;
        }

        Dictionary<Guid, string> names = query.Dimension switch
        {
            OperationsReportDimension.Customer => await CustomerNamesAsync(invoices.Select(item => item.Key.CustomerId).Distinct().ToList(), cancellationToken),
            OperationsReportDimension.Warehouse => query.Places.ToDictionary(place => place.Id, place => place.Name),
            _ => new Dictionary<Guid, string>()
        };
        return Ranked(invoices.GroupBy(item => query.Dimension switch
            {
                OperationsReportDimension.Service => item.Key.ServiceCode ?? string.Empty,
                OperationsReportDimension.Route => item.Key.Route ?? string.Empty,
                OperationsReportDimension.Customer => item.Key.CustomerId.ToString(),
                _ => item.Key.WarehouseId.ToString()
            }, StringComparer.Ordinal)
            .Select(group => (Label(group.Key, query.Dimension, names), Hide(query, new decimal?[] { group.Sum(item => item.Count), group.Sum(item => item.Subtotal), group.Sum(item => item.Tax),
                group.Sum(item => item.Total) }))), query.Money ? 3 : 0, 4) is var ranked ? ranked with { Totals = Hide(query, ranked.Totals.ToArray()) } : null!;
    }

    private static OperationsReportLabel Label(string key, OperationsReportDimension dimension, Dictionary<Guid, string> names) =>
        key.Length == 0 ? new OperationsReportLabel(TextKey: "ui.reports.none")
            : Guid.TryParse(key, out Guid id) ? new OperationsReportLabel(Text: names.TryGetValue(id, out string? name) ? name : key)
            : new OperationsReportLabel(Text: key);

    private async Task<Dictionary<Guid, string>> CustomerNamesAsync(List<Guid> ids, CancellationToken cancellationToken)
    {
        Dictionary<Guid, string> names = new();

        foreach (Guid[] chunk in ids.Chunk(1000))
        {
            foreach (var customer in await _context.Customers.AsNoTracking().Where(item => chunk.Contains(item.Id))
                .Select(item => new { item.Id, item.Code, item.FirstName, item.LastName, item.CompanyName }).ToListAsync(cancellationToken))
            {
                names[customer.Id] = (customer.Code is { } code ? code + " · " : string.Empty) + (customer.CompanyName ?? (customer.FirstName + " " + customer.LastName));
            }
        }

        return names;
    }

    // What is owed now, aged at the end date: not yet due, then 1-30, 31-60, 61-90 and over 90 days past due.
    private static decimal?[] Ages(IEnumerable<(DateOnly Due, decimal Owed)> items, DateOnly asOf)
    {
        decimal[] buckets = new decimal[6];

        foreach ((DateOnly due, decimal owed) in items)
        {
            int late = asOf.DayNumber - due.DayNumber;
            buckets[late <= 0 ? 0 : late <= 30 ? 1 : late <= 60 ? 2 : late <= 90 ? 3 : 4] += owed;
            buckets[5] += owed;
        }

        return buckets.Select(value => (decimal?)value).ToArray();
    }

    internal async Task<OperationsReportTable> ReceivablesAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        List<Guid> places = query.Places.Select(place => place.Id).ToList();
        var owed = await _context.Invoices.AsNoTracking()
            .Where(item => item.Status == OperationsInvoiceStatus.Issued && item.IssuedOn <= query.To && places.Contains(item.WarehouseId)
                && item.Total - item.CreditedTotal - item.PaidTotal - item.PendingTotal + item.RefundedTotal > 0m)
            .Select(item => new { item.CustomerId, Due = item.DueOn ?? item.IssuedOn, Owed = item.Total - item.CreditedTotal - item.PaidTotal - item.PendingTotal + item.RefundedTotal })
            .ToListAsync(cancellationToken);
        Dictionary<Guid, string> names = await CustomerNamesAsync(owed.Select(item => item.CustomerId).Distinct().ToList(), cancellationToken);
        return Ranked(owed.GroupBy(item => item.CustomerId).Select(group => (new OperationsReportLabel(Text: names.GetValueOrDefault(group.Key, group.Key.ToString())),
            Ages(group.Select(item => (item.Due, item.Owed)), query.To))), 5, 6);
    }

    internal async Task<OperationsReportTable> PayablesAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        var owed = await _context.Bills.AsNoTracking()
            .Where(item => (item.Status == OperationsBillStatus.Entered || item.Status == OperationsBillStatus.Approved) && item.BilledOn <= query.To && item.Amount - item.PaidTotal > 0m)
            .Select(item => new { item.PartyName, item.DueOn, Owed = item.Amount - item.PaidTotal })
            .ToListAsync(cancellationToken);
        return Ranked(owed.GroupBy(item => item.PartyName, StringComparer.Ordinal).Select(group => (new OperationsReportLabel(Text: group.Key),
            Ages(group.Select(item => (item.DueOn, item.Owed)), query.To))), 5, 6);
    }

    // Settled and still-awaited amounts per method (or warehouse, or period), each payment on its warehouse's local day.
    internal async Task<OperationsReportTable> PaymentsAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        (DateTimeOffset fromUtc, DateTimeOffset toUtc) = Range(query);
        List<Guid> places = query.Places.Select(place => place.Id).ToList();
        Dictionary<Guid, TimeZoneInfo> zones = Zones(query);
        List<OperationsPayment> payments = (await _context.Payments.AsNoTracking()
                .Where(item => places.Contains(item.WarehouseId) && item.TakenUtc >= fromUtc && item.TakenUtc < toUtc).ToListAsync(cancellationToken))
            .Where(item => Between(query, Local(item.TakenUtc, zones, item.WarehouseId))).ToList();
        var tenders = payments.SelectMany(payment => payment.Tenders.Select(tender => new { payment.Id, payment.WarehouseId, Day = Local(payment.TakenUtc, zones, payment.WarehouseId), tender }))
            .Where(item => item.tender.Status != OperationsTenderStatus.NotReceived).ToList();

        if (query.Dimension == OperationsReportDimension.Period)
        {
            OperationsReportTable table = OverTime(query, 3, add =>
            {
                foreach (OperationsPayment payment in payments)
                {
                    add(Local(payment.TakenUtc, zones, payment.WarehouseId), 0, 1);
                }

                foreach (var item in tenders)
                {
                    add(item.Day, item.tender.Status == OperationsTenderStatus.Settled ? 1 : 2, item.tender.Amount);
                }
            });
            return table with { Rows = table.Rows.Select(row => row with { Values = Hide(query, row.Values.ToArray()) }).ToList(), Totals = Hide(query, table.Totals.ToArray()) };
        }

        Dictionary<Guid, string> names = query.Places.ToDictionary(place => place.Id, place => place.Name);
        OperationsReportTable ranked = Ranked(tenders.GroupBy(item => query.Dimension == OperationsReportDimension.Warehouse ? item.WarehouseId.ToString() : item.tender.Method.ToString(),
                StringComparer.Ordinal)
            .Select(group => (query.Dimension == OperationsReportDimension.Warehouse ? new OperationsReportLabel(Text: names.GetValueOrDefault(Guid.Parse(group.Key), group.Key))
                    : new OperationsReportLabel(TextKey: "ui.reports.method." + group.Key),
                Hide(query, new decimal?[]
                {
                    group.Select(item => item.Id).Distinct().Count(), group.Where(item => item.tender.Status == OperationsTenderStatus.Settled).Sum(item => item.tender.Amount),
                    group.Where(item => item.tender.Status == OperationsTenderStatus.AwaitingConfirmation).Sum(item => item.tender.Amount)
                }))), query.Money ? 1 : 0, 3);
        return ranked with { Totals = Hide(query, new decimal?[] { payments.Count, ranked.Totals[1], ranked.Totals[2] }) };
    }

    internal async Task<OperationsReportTable> TaxesAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        List<Guid> places = query.Places.Select(place => place.Id).ToList();
        var taxes = (await _context.Invoices.AsNoTracking()
                .Where(item => item.Status == OperationsInvoiceStatus.Issued && item.IssuedOn >= query.From && item.IssuedOn <= query.To && places.Contains(item.WarehouseId))
                .Select(item => item.Taxes).ToListAsync(cancellationToken))
            .SelectMany(list => list).ToList();
        return Ranked(taxes.GroupBy(tax => (tax.Kind, tax.RatePercent)).Select(group => (
            new OperationsReportLabel(Text: group.Key.RatePercent.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + " %", TextKey: "ui.reports.tax." + group.Key.Kind),
            Hide(query, new decimal?[] { group.Sum(tax => tax.Basis), group.Sum(tax => tax.Amount), group.Sum(tax => tax.Credited), group.Sum(tax => tax.Amount - tax.Credited) }))), 1, 4)
            is var table ? table with { Totals = Hide(query, table.Totals.ToArray()) } : null!;
    }

    // ---------------------------------------------------------------- customers

    internal async Task<OperationsReportTable> SignupsAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        (DateTimeOffset fromUtc, DateTimeOffset toUtc) = Range(query);
        TimeZoneInfo zone = Zone(query.CompanyTimeZone);
        List<DateTimeOffset> created = await _context.Customers.AsNoTracking().Where(item => item.CreatedUtc >= fromUtc && item.CreatedUtc < toUtc)
            .Select(item => item.CreatedUtc).ToListAsync(cancellationToken);
        var booked = await _context.Shipments.AsNoTracking().Where(item => item.BookedUtc >= fromUtc && item.BookedUtc < toUtc)
            .Select(item => new { item.CustomerId, Booked = item.BookedUtc!.Value }).ToListAsync(cancellationToken);
        HashSet<(DateOnly, Guid)> active = new();
        OperationsReportTable table = OverTime(query, 2, add =>
        {
            foreach (DateTimeOffset utc in created)
            {
                add(Local(utc, zone), 0, 1);
            }

            foreach (var item in booked)
            {
                DateOnly day = Local(item.Booked, zone);

                if (Between(query, day) && active.Add((OperationsReportPeriods.Start(day, query.Grain), item.CustomerId)))
                {
                    add(day, 1, 1);
                }
            }
        });

        // Active over the whole period: each customer once.
        return table with
        {
            Totals = new decimal?[] { table.Totals[0], booked.Where(item => Between(query, Local(item.Booked, zone))).Select(item => item.CustomerId).Distinct().Count() }
        };
    }

    internal async Task<OperationsReportTable> TopCustomersAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        (DateTimeOffset fromUtc, DateTimeOffset toUtc) = Range(query);
        List<Guid> places = query.Places.Select(place => place.Id).ToList();
        Dictionary<Guid, TimeZoneInfo> zones = Zones(query);
        var shipments = (await _context.Shipments.AsNoTracking()
                .Where(item => places.Contains(item.OriginWarehouseId) && item.BookedUtc >= fromUtc && item.BookedUtc < toUtc)
                .Select(item => new
                {
                    item.CustomerId,
                    item.OriginWarehouseId,
                    Booked = item.BookedUtc!.Value,
                    Parcels = _context.Parcels.Count(parcel => parcel.ShipmentId == item.Id),
                    Weight = _context.Parcels.Where(parcel => parcel.ShipmentId == item.Id).Sum(parcel => parcel.WeightKg) ?? 0m
                }).ToListAsync(cancellationToken))
            .Where(item => Between(query, Local(item.Booked, zones, item.OriginWarehouseId))).ToList();
        Dictionary<Guid, decimal> revenue = query.Money
            ? await _context.Invoices.AsNoTracking()
                .Where(item => item.Status == OperationsInvoiceStatus.Issued && item.IssuedOn >= query.From && item.IssuedOn <= query.To && places.Contains(item.WarehouseId))
                .GroupBy(item => item.CustomerId).Select(group => new { group.Key, Total = group.Sum(item => item.Total) })
                .ToDictionaryAsync(item => item.Key, item => item.Total, cancellationToken)
            : new Dictionary<Guid, decimal>();
        List<Guid> customers = shipments.Select(item => item.CustomerId).Concat(revenue.Keys).Distinct().ToList();
        Dictionary<Guid, string> names = await CustomerNamesAsync(customers, cancellationToken);
        List<(OperationsReportLabel, decimal?[])> rows = customers.Select(customer =>
        {
            var mine = shipments.Where(item => item.CustomerId == customer).ToList();
            return (new OperationsReportLabel(Text: names.GetValueOrDefault(customer, customer.ToString())), Hide(query, new decimal?[]
            {
                mine.Count, mine.Sum(item => item.Parcels), mine.Sum(item => item.Weight), revenue.GetValueOrDefault(customer)
            }));
        }).ToList();
        OperationsReportTable ranked = Ranked(rows, query.Money ? 3 : 2, 4);
        return ranked with { Rows = ranked.Rows.Take(50).ToList(), Totals = Hide(query, ranked.Totals.ToArray()), Truncated = ranked.Rows.Count > 50 };
    }

    // Customers who shipped before but not in the last N days (as of the end date).
    internal async Task<OperationsReportTable> InactiveCustomersAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        // The end of the last day in the company's time zone (review).
        TimeZoneInfo zone = Zone(query.CompanyTimeZone);
        DateTime midnight = query.To.AddDays(1).ToDateTime(TimeOnly.MinValue);
        DateTimeOffset end = new DateTimeOffset(midnight, zone.GetUtcOffset(midnight)).ToUniversalTime();
        DateTimeOffset since = end.AddDays(-query.Days);
        var last = await _context.Shipments.AsNoTracking().Where(item => item.BookedUtc != null && item.BookedUtc < end)
            .GroupBy(item => item.CustomerId)
            .Select(group => new { group.Key, Count = group.Count(), Last = group.Max(item => item.BookedUtc!.Value) })
            .Where(item => item.Last < since)
            .ToListAsync(cancellationToken);
        Dictionary<Guid, string> names = await CustomerNamesAsync(last.Select(item => item.Key).ToList(), cancellationToken);
        List<(OperationsReportLabel, decimal?[])> rows = last.Select(item => (new OperationsReportLabel(Text: names.GetValueOrDefault(item.Key, item.Key.ToString())),
            new decimal?[] { item.Count, Math.Floor((decimal)(end - item.Last).TotalDays) })).ToList();
        OperationsReportTable ranked = Ranked(rows, 0, 2);
        return ranked with { Totals = new decimal?[] { ranked.Totals[0], null } };
    }

    // ---------------------------------------------------------------- carriers and warehouse

    internal async Task<OperationsReportTable> CarrierCostAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        (DateTimeOffset fromUtc, DateTimeOffset toUtc) = Range(query);
        Dictionary<Guid, TimeZoneInfo> zones = Zones(query);
        var dispatches = (await _context.Dispatches.AsNoTracking().Where(item => item.DepartedUtc >= fromUtc && item.DepartedUtc < toUtc).ToListAsync(cancellationToken))
            .Where(item => Between(query, Local(item.DepartedUtc!.Value, zones, item.WarehouseId)))
            .Select(item => new { Carrier = item.Legs.OrderBy(leg => leg.Sequence).FirstOrDefault()?.CarrierId, Estimated = item.CostTotal ?? item.CostComputed, item.BilledTotal })
            .ToList();
        Dictionary<Guid, string> carriers = await _context.Carriers.AsNoTracking().ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        return Ranked(dispatches.GroupBy(item => item.Carrier).Select(group => (
            group.Key is { } id && carriers.TryGetValue(id, out string? name) ? new OperationsReportLabel(Text: name) : new OperationsReportLabel(TextKey: "ui.reports.none"),
            Hide(query, new decimal?[]
            {
                group.Count(), group.Sum(item => item.Estimated ?? 0m), group.Sum(item => item.BilledTotal ?? 0m),
                group.Where(item => item.BilledTotal is not null).Sum(item => item.BilledTotal!.Value - (item.Estimated ?? 0m))
            }))), 0, 4) is var table ? table with { Totals = Hide(query, table.Totals.ToArray()) } : null!;
    }

    // Hours from departure to arrival of each leg that arrived in the period, per carrier and route.
    internal async Task<OperationsReportTable> TransitAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        (DateTimeOffset fromUtc, DateTimeOffset toUtc) = Range(query);
        Dictionary<Guid, TimeZoneInfo> zones = Zones(query);
        List<Vixly.Operations.Domain.Dispatch.OperationsDispatch> dispatches = await _context.Dispatches.AsNoTracking()
            .Where(item => item.DepartedUtc >= fromUtc.AddDays(-90) && item.DepartedUtc < toUtc).ToListAsync(cancellationToken);
        Dictionary<Guid, string> carriers = await _context.Carriers.AsNoTracking().ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);
        var legs = dispatches.SelectMany(dispatch => dispatch.Legs.Select(leg => (dispatch.WarehouseId, leg)))
            .Where(item => item.leg.DepartedUtc is not null && item.leg.ArrivedUtc is { } arrived && Between(query, Local(arrived, zones, item.WarehouseId)))
            .Select(item => new
            {
                Key = (item.leg.CarrierId is { } id && carriers.TryGetValue(id, out string? name) ? name : item.leg.Kind.ToString()) + " · " + item.leg.From + " → " + item.leg.To,
                Hours = (item.leg.ArrivedUtc!.Value - item.leg.DepartedUtc!.Value).TotalHours
            }).ToList();
        decimal?[] Spread3(IEnumerable<double> hours)
        {
            decimal?[] spread = Spread(hours.ToList());
            return new[] { spread[0], spread[1], spread[2] };
        }

        return Ranked(legs.GroupBy(item => item.Key, StringComparer.Ordinal).Select(group => (new OperationsReportLabel(Text: group.Key), Spread3(group.Select(item => item.Hours)))), 0, 3)
            is var table ? table with { Totals = Spread3(legs.Select(item => item.Hours)) } : null!;
    }

    // Parcels on the shelves now (received, not yet departed), by days since they arrived.
    internal async Task<OperationsReportTable> StockAgeAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        List<Guid> places = query.Places.Select(place => place.Id).ToList();
        var parcels = await _context.Parcels.AsNoTracking()
            .Where(item => item.WarehouseId != null && places.Contains(item.WarehouseId.Value) && item.Milestone >= OperationsMilestone.ReceivedAtOrigin
                && item.Milestone < OperationsMilestone.DepartedOrigin)
            .Select(item => new
            {
                WarehouseId = item.WarehouseId!.Value,
                // Aged from when it was received, not from its pre-alert (review).
                CreatedUtc = _context.TrackingEvents.Where(tracking => tracking.SubjectKind == OperationsTrackingSubject.Parcel && tracking.SubjectId == item.Id
                    && tracking.Code == OperationsTrackingCodes.ReceivedAtOrigin).Min(tracking => (DateTimeOffset?)tracking.OccurredUtc) ?? item.CreatedUtc,
                Unclaimed = item.CustomerId == null
            })
            .ToListAsync(cancellationToken);
        decimal?[] Ages(IEnumerable<(DateTimeOffset Since, bool Unclaimed)> items)
        {
            decimal[] buckets = new decimal[7];

            foreach ((DateTimeOffset since, bool unclaimed) in items)
            {
                double days = (query.UtcNow - since).TotalDays;
                buckets[0]++;
                buckets[days <= 7 ? 1 : days <= 14 ? 2 : days <= 30 ? 3 : days <= 60 ? 4 : 5]++;
                buckets[6] += unclaimed ? 1 : 0;
            }

            return buckets.Select(value => (decimal?)value).ToArray();
        }

        List<OperationsReportRow> rows = query.Places.Select(place => new OperationsReportRow(new OperationsReportLabel(Text: place.Name),
            Ages(parcels.Where(item => item.WarehouseId == place.Id).Select(item => (item.CreatedUtc, item.Unclaimed))))).ToList();
        return new OperationsReportTable(rows, Ages(parcels.Select(item => (item.CreatedUtc, item.Unclaimed))));
    }

    internal async Task<OperationsReportTable> SuppliesValueAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        List<Guid> places = query.Places.Select(place => place.Id).ToList();
        var stocks = await _context.SupplyStocks.AsNoTracking().Where(item => places.Contains(item.WarehouseId) && item.OnHand > 0)
            .Select(item => new { item.SupplyId, item.WarehouseId, item.OnHand }).ToListAsync(cancellationToken);
        List<Guid> ids = stocks.Select(item => item.SupplyId).Distinct().ToList();
        Dictionary<Guid, Vixly.Operations.Domain.Supplies.OperationsSupply> supplies = await _context.Supplies.AsNoTracking().Where(item => ids.Contains(item.Id))
            .ToDictionaryAsync(item => item.Id, cancellationToken);
        decimal?[] Value(IEnumerable<(Guid Supply, Guid Warehouse, int OnHand)> items)
        {
            List<(Guid Supply, Guid Warehouse, int OnHand)> list = items.ToList();
            return Hide(query, new decimal?[]
            {
                list.Select(item => item.Supply).Distinct().Count(), list.Sum(item => item.OnHand),
                list.Sum(item => item.OnHand * (supplies.TryGetValue(item.Supply, out Vixly.Operations.Domain.Supplies.OperationsSupply? supply)
                    ? supply.UnitPrice(item.Warehouse, 1, query.UtcNow) ?? 0m : 0m))
            });
        }

        List<OperationsReportRow> rows = query.Places.Select(place => new OperationsReportRow(new OperationsReportLabel(Text: place.Name),
            Value(stocks.Where(item => item.WarehouseId == place.Id).Select(item => (item.SupplyId, item.WarehouseId, item.OnHand))))).ToList();
        return new OperationsReportTable(rows, Value(stocks.Select(item => (item.SupplyId, item.WarehouseId, item.OnHand))));
    }
}
