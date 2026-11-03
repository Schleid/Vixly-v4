using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Payables;
using Vixly.Operations.Domain.Tracking;
using Vixly.Operations.Infrastructure.Persistence;

namespace Vixly.Operations.Infrastructure.Reports;

// =========================================================
// FILE: OperationsReportReader.cs
//
// PURPOSE:
// Works out the figures of the company's reports (Slice 11; owner, 2026-10-22).
//
// RESPONSIBILITIES:
// - One method per report: the database counts what is large (tracking events, invoices), the rest is added up here
// - Days in each warehouse's own time zone (AT TIME ZONE in the database, TimeZoneInfo here); cuts by day, week or month
//
// IMPORTANT NOTES:
// - Read only, not tracked, tenant-filtered (raw SQL names the tenant itself)
// - Voided tracking events are not counted; voided invoices are not sales
// - Money values are left empty when the reader of the report may not see them
// =========================================================
internal sealed class OperationsReportReader : IOperationsReportReader
{
    private readonly OperationsDbContext _context;

    private readonly IOperationsTenantScope _scope;

    private readonly OperationsReportReaderMoney _money;

    public OperationsReportReader(OperationsDbContext context, IOperationsTenantScope scope)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _money = new OperationsReportReaderMoney(context);
    }

    public Task<OperationsReportTable> RunAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        return query.Report.Key switch
        {
            "activity" => ActivityAsync(query, cancellationToken),
            "shipments" => ShipmentsAsync(query, cancellationToken),
            "dwell" => DwellAsync(query, cancellationToken),
            "holds" => HoldsAsync(query, cancellationToken),
            "on_time" => OnTimeAsync(query, cancellationToken),
            "sales" => _money.SalesAsync(query, cancellationToken),
            "receivables" => _money.ReceivablesAsync(query, cancellationToken),
            "payables" => _money.PayablesAsync(query, cancellationToken),
            "payments" => _money.PaymentsAsync(query, cancellationToken),
            "taxes" => _money.TaxesAsync(query, cancellationToken),
            "signups" => _money.SignupsAsync(query, cancellationToken),
            "top_customers" => _money.TopCustomersAsync(query, cancellationToken),
            "inactive_customers" => _money.InactiveCustomersAsync(query, cancellationToken),
            "carrier_cost" => _money.CarrierCostAsync(query, cancellationToken),
            "transit" => _money.TransitAsync(query, cancellationToken),
            "stock_age" => _money.StockAgeAsync(query, cancellationToken),
            "supplies_value" => _money.SuppliesValueAsync(query, cancellationToken),
            _ => Task.FromResult(new OperationsReportTable(Array.Empty<OperationsReportRow>(), Array.Empty<decimal?>()))
        };
    }

    // ---------------------------------------------------------------- operations

    private static readonly (string Column, string Code)[] ActivityCodes =
    {
        ("received", OperationsTrackingCodes.ReceivedAtOrigin), ("inspected", OperationsTrackingCodes.InspectionCompleted), ("holds_opened", OperationsTrackingCodes.HoldOpened),
        ("departed", OperationsTrackingCodes.DepartedOrigin), ("arrived", OperationsTrackingCodes.ArrivedDestinationWarehouse), ("ready", OperationsTrackingCodes.ReadyForPickup),
        ("picked_up", OperationsTrackingCodes.PickedUp), ("delivered", OperationsTrackingCodes.Delivered), ("delivery_failed", OperationsTrackingCodes.DeliveryFailed)
    };

    // Parcels with each step, per local day of the warehouse where it happened.
    private async Task<OperationsReportTable> ActivityAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        Dictionary<(string Code, DateOnly Day), decimal> counts = new();

        foreach (IGrouping<string, OperationsReportPlace> zone in query.Places.GroupBy(place => place.TimeZone, StringComparer.Ordinal))
        {
            List<SqlParameter> parameters = Parameters(query, zone);
            string codes = string.Join(", ", ActivityCodes.Select((code, index) =>
            {
                parameters.Add(new SqlParameter("@code" + index, code.Code));
                return "@code" + index;
            }));
            string sql = $"""
                SELECT e.[Code] AS [Code], CAST(e.[OccurredUtc] AT TIME ZONE @zone AS date) AS [Day], COUNT(DISTINCT e.[SubjectId]) AS [Count]
                FROM [ops].[TrackingEvents] e
                WHERE e.[TenantId] = @tenant AND e.[SubjectKind] = 1 AND e.[Code] IN ({codes}) AND e.[LocationCode] IN ({Locations(zone, parameters)})
                  AND e.[OccurredUtc] >= @fromUtc AND e.[OccurredUtc] < @toUtc
                  AND NOT EXISTS (SELECT 1 FROM [ops].[TrackingEvents] v WHERE v.[TenantId] = e.[TenantId] AND v.[VoidsEventId] = e.[Id])
                GROUP BY e.[Code], CAST(e.[OccurredUtc] AT TIME ZONE @zone AS date)
                """;

            foreach (CodeDayCount row in await InZoneAsync<CodeDayCount>(sql, parameters, cancellationToken))
            {
                DateOnly day = DateOnly.FromDateTime(row.Day);

                if (day >= query.From && day <= query.To)
                {
                    counts[(row.Code, day)] = counts.GetValueOrDefault((row.Code, day)) + row.Count;
                }
            }
        }

        return OverTime(query, ActivityCodes.Length, add => counts.Select(entry => (entry.Key.Day, Array.FindIndex(ActivityCodes, code => code.Code == entry.Key.Code), entry.Value))
            .ToList().ForEach(item => add(item.Day, item.Item2, item.Value)));
    }

    private async Task<OperationsReportTable> ShipmentsAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        (DateTimeOffset fromUtc, DateTimeOffset toUtc) = Range(query);
        List<Guid> places = query.Places.Select(place => place.Id).ToList();
        Dictionary<Guid, TimeZoneInfo> zones = Zones(query);
        var created = await _context.Shipments.AsNoTracking()
            .Where(item => places.Contains(item.OriginWarehouseId) && item.CreatedUtc >= fromUtc && item.CreatedUtc < toUtc)
            .Select(item => new { item.OriginWarehouseId, item.CreatedUtc }).ToListAsync(cancellationToken);
        var booked = await _context.Shipments.AsNoTracking()
            .Where(item => places.Contains(item.OriginWarehouseId) && item.BookedUtc >= fromUtc && item.BookedUtc < toUtc)
            .Select(item => new
            {
                item.OriginWarehouseId,
                Booked = item.BookedUtc!.Value,
                Parcels = _context.Parcels.Count(parcel => parcel.ShipmentId == item.Id),
                Weight = _context.Parcels.Where(parcel => parcel.ShipmentId == item.Id).Sum(parcel => parcel.WeightKg) ?? 0m
            }).ToListAsync(cancellationToken);

        return OverTime(query, 4, add =>
        {
            foreach (var item in created)
            {
                add(Local(item.CreatedUtc, zones, item.OriginWarehouseId), 0, 1);
            }

            foreach (var item in booked)
            {
                DateOnly day = Local(item.Booked, zones, item.OriginWarehouseId);
                add(day, 1, 1);
                add(day, 2, item.Parcels);
                add(day, 3, item.Weight);
            }
        });
    }

    // Days from received to departed of the parcels that left in the period, per warehouse.
    private async Task<OperationsReportTable> DwellAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        Dictionary<string, List<double>> days = new(StringComparer.Ordinal);

        foreach (IGrouping<string, OperationsReportPlace> zone in query.Places.GroupBy(place => place.TimeZone, StringComparer.Ordinal))
        {
            List<SqlParameter> parameters = Parameters(query, zone);
            parameters.Add(new SqlParameter("@departed", OperationsTrackingCodes.DepartedOrigin));
            parameters.Add(new SqlParameter("@received", OperationsTrackingCodes.ReceivedAtOrigin));
            string sql = $"""
                SELECT d.[LocationCode] AS [Location], CAST(d.[OccurredUtc] AT TIME ZONE @zone AS date) AS [Day],
                    DATEDIFF(minute, MIN(r.[OccurredUtc]), MIN(d.[OccurredUtc])) AS [Minutes]
                FROM [ops].[TrackingEvents] d
                JOIN [ops].[TrackingEvents] r ON r.[TenantId] = d.[TenantId] AND r.[SubjectKind] = 1 AND r.[SubjectId] = d.[SubjectId] AND r.[Code] = @received
                    AND NOT EXISTS (SELECT 1 FROM [ops].[TrackingEvents] rv WHERE rv.[TenantId] = r.[TenantId] AND rv.[VoidsEventId] = r.[Id])
                WHERE d.[TenantId] = @tenant AND d.[SubjectKind] = 1 AND d.[Code] = @departed AND d.[LocationCode] IN ({Locations(zone, parameters)})
                  AND d.[OccurredUtc] >= @fromUtc AND d.[OccurredUtc] < @toUtc
                  AND NOT EXISTS (SELECT 1 FROM [ops].[TrackingEvents] v WHERE v.[TenantId] = d.[TenantId] AND v.[VoidsEventId] = d.[Id])
                GROUP BY d.[SubjectId], d.[LocationCode], CAST(d.[OccurredUtc] AT TIME ZONE @zone AS date)
                """;

            foreach (LocationDayMinutes row in await InZoneAsync<LocationDayMinutes>(sql, parameters, cancellationToken))
            {
                DateOnly day = DateOnly.FromDateTime(row.Day);

                if (day >= query.From && day <= query.To && row.Minutes >= 0)
                {
                    (days.TryGetValue(row.Location, out List<double>? list) ? list : days[row.Location] = new List<double>()).Add(row.Minutes / 1440d);
                }
            }
        }

        List<OperationsReportRow> rows = query.Places.Where(place => days.ContainsKey(place.Code))
            .Select(place => new OperationsReportRow(new OperationsReportLabel(Text: place.Name), Spread(days[place.Code]))).ToList();
        return new OperationsReportTable(rows, Spread(days.Values.SelectMany(list => list).ToList()));
    }

    // Holds opened in the period by reason; how many were released and how long they lasted.
    private async Task<OperationsReportTable> HoldsAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        (DateTimeOffset fromUtc, DateTimeOffset toUtc) = Range(query);
        List<string> codes = query.Places.Select(place => place.Code).ToList();
        Dictionary<string, TimeZoneInfo> zones = query.Places.ToDictionary(place => place.Code, place => Zone(place.TimeZone), StringComparer.Ordinal);
        List<OperationsTrackingEvent> opened = await _context.TrackingEvents.AsNoTracking()
            .Where(item => item.SubjectKind == OperationsTrackingSubject.Parcel && item.Code == OperationsTrackingCodes.HoldOpened && item.LocationCode != null
                && codes.Contains(item.LocationCode) && item.OccurredUtc >= fromUtc && item.OccurredUtc < toUtc
                && !_context.TrackingEvents.Any(voiding => voiding.VoidsEventId == item.Id))
            .ToListAsync(cancellationToken);
        opened = opened.Where(item => Between(query, Local(item.OccurredUtc, zones[item.LocationCode!]))).ToList();
        List<Guid> subjects = opened.Select(item => item.SubjectId).Distinct().ToList();
        List<OperationsTrackingEvent> released = new();

        foreach (Guid[] chunk in subjects.Chunk(1000))
        {
            released.AddRange(await _context.TrackingEvents.AsNoTracking()
                .Where(item => item.SubjectKind == OperationsTrackingSubject.Parcel && item.Code == OperationsTrackingCodes.HoldReleased && chunk.Contains(item.SubjectId)
                    && !_context.TrackingEvents.Any(voiding => voiding.VoidsEventId == item.Id))
                .ToListAsync(cancellationToken));
        }

        ILookup<Guid, OperationsTrackingEvent> releases = released.ToLookup(release => release.SubjectId);
        var pairs = opened.Select(open => new
        {
            Reason = open.HoldReason(),
            Release = releases[open.SubjectId].Where(release => release.OccurredUtc >= open.OccurredUtc
                    && (release.HoldReason() == open.HoldReason() || release.HoldReason() == OperationsHoldReasons.None))
                .OrderBy(release => release.OccurredUtc).FirstOrDefault(),
            Open = open.OccurredUtc
        }).ToList();
        List<OperationsReportRow> rows = pairs.GroupBy(pair => pair.Reason).OrderByDescending(group => group.Count())
            .Select(group => new OperationsReportRow(new OperationsReportLabel(TextKey: "ui.reports.hold." + group.Key), new decimal?[]
            {
                group.Count(), group.Count(pair => pair.Release is not null),
                Average(group.Where(pair => pair.Release is not null).Select(pair => (pair.Release!.OccurredUtc - pair.Open).TotalHours))
            })).ToList();
        return new OperationsReportTable(rows, new decimal?[]
        {
            pairs.Count, pairs.Count(pair => pair.Release is not null), Average(pairs.Where(pair => pair.Release is not null).Select(pair => (pair.Release!.OccurredUtc - pair.Open).TotalHours))
        });
    }

    // Legs that departed by their planned time (and the company's grace), and parcels made ready within their service's target.
    private async Task<OperationsReportTable> OnTimeAsync(OperationsReportQuery query, CancellationToken cancellationToken)
    {
        (DateTimeOffset fromUtc, DateTimeOffset toUtc) = Range(query);
        List<Guid> places = query.Places.Select(place => place.Id).ToList();
        Dictionary<Guid, TimeZoneInfo> zones = Zones(query);
        List<OperationsDispatch> dispatches = await _context.Dispatches.AsNoTracking()
            .Where(item => places.Contains(item.WarehouseId) && item.DepartedUtc >= fromUtc.AddDays(-90) && item.DepartedUtc < toUtc)
            .ToListAsync(cancellationToken);
        List<OperationsReportRow> rows = new();
        TimeSpan grace = TimeSpan.FromHours(query.GraceHours);
        (int Measured, int OnTime) all = (0, 0);

        foreach (OperationsCarrierKind kind in Enum.GetValues<OperationsCarrierKind>())
        {
            List<bool> legs = dispatches.SelectMany(dispatch => dispatch.Legs.Select(leg => (dispatch.WarehouseId, leg)))
                .Where(item => item.leg.Kind == kind && item.leg.DepartedUtc is { } departed && item.leg.ScheduledDepartureUtc is not null
                    && Between(query, Local(departed, zones, item.WarehouseId)))
                .Select(item => item.leg.DepartedUtc!.Value <= item.leg.ScheduledDepartureUtc!.Value + grace).ToList();

            if (legs.Count > 0)
            {
                rows.Add(OnTimeRow("ui.reports.on_time.legs_" + kind.ToString().ToLowerInvariant(), legs.Count, legs.Count(onTime => onTime)));
                all = (all.Measured + legs.Count, all.OnTime + legs.Count(onTime => onTime));
            }
        }

        // Ready: from arrival at the destination warehouse to ready, against the target days of the shipment's service.
        Dictionary<string, int> targets = await _context.ShippingServiceSetups.AsNoTracking().Where(item => item.ReadyTargetDays != null)
            .ToDictionaryAsync(item => item.ServiceCode, item => item.ReadyTargetDays!.Value, StringComparer.Ordinal, cancellationToken);

        if (targets.Count > 0)
        {
            var ready = await _context.Parcels.AsNoTracking()
                .Where(item => item.WarehouseId != null && places.Contains(item.WarehouseId.Value) && item.ReadyUtc >= fromUtc && item.ReadyUtc < toUtc && item.ShipmentId != null)
                .Select(item => new
                {
                    item.Id,
                    WarehouseId = item.WarehouseId!.Value,
                    Ready = item.ReadyUtc!.Value,
                    Service = _context.Shipments.Where(shipment => shipment.Id == item.ShipmentId).Select(shipment => shipment.ServiceCode).FirstOrDefault(),
                    Arrived = _context.TrackingEvents.Where(tracking => tracking.SubjectKind == OperationsTrackingSubject.Parcel && tracking.SubjectId == item.Id
                        && tracking.Code == OperationsTrackingCodes.ArrivedDestinationWarehouse && !_context.TrackingEvents.Any(voiding => voiding.VoidsEventId == tracking.Id))
                        .Min(tracking => (DateTimeOffset?)tracking.OccurredUtc)
                }).ToListAsync(cancellationToken);
            List<bool> measured = ready.Where(item => item.Arrived is not null && item.Service is not null && targets.ContainsKey(item.Service)
                    && Between(query, Local(item.Ready, zones, item.WarehouseId)))
                .Select(item => (item.Ready - item.Arrived!.Value).TotalDays <= targets[item.Service!]).ToList();

            if (measured.Count > 0)
            {
                rows.Add(OnTimeRow("ui.reports.on_time.ready", measured.Count, measured.Count(onTime => onTime)));
                all = (all.Measured + measured.Count, all.OnTime + measured.Count(onTime => onTime));
            }
        }

        return new OperationsReportTable(rows, OnTimeRow(string.Empty, all.Measured, all.OnTime).Values);
    }

    private static OperationsReportRow OnTimeRow(string key, int measured, int onTime) =>
        new(new OperationsReportLabel(TextKey: key), new decimal?[] { measured, onTime, measured - onTime, measured == 0 ? null : Math.Round(100m * onTime / measured, 1) });

    // ---------------------------------------------------------------- shared

    private Guid Tenant() => _scope.TenantId ?? throw new InvalidOperationException("Reports are read in a tenant scope (D-F1).");

    private List<SqlParameter> Parameters(OperationsReportQuery query, IGrouping<string, OperationsReportPlace> zone)
    {
        (DateTimeOffset fromUtc, DateTimeOffset toUtc) = Range(query);
        return new List<SqlParameter>
        {
            new("@tenant", Tenant()),
            new("@zone", SqlZone(zone.Key)),
            new("@fromUtc", fromUtc),
            new("@toUtc", toUtc)
        };
    }

    private static string Locations(IEnumerable<OperationsReportPlace> places, List<SqlParameter> parameters) =>
        string.Join(", ", places.Select(place =>
        {
            string name = "@location" + parameters.Count;
            parameters.Add(new SqlParameter(name, place.Code));
            return name;
        }));

    // Wide enough for every time zone (UTC-12 to UTC+14); local days are checked after.
    internal static (DateTimeOffset FromUtc, DateTimeOffset ToUtc) Range(OperationsReportQuery query) =>
        (new DateTimeOffset(query.From.ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddHours(-14), new DateTimeOffset(query.To.AddDays(1).ToDateTime(TimeOnly.MinValue), TimeSpan.Zero).AddHours(14));

    internal static bool Between(OperationsReportQuery query, DateOnly day) => day >= query.From && day <= query.To;

    internal static TimeZoneInfo Zone(string? id) => id is { Length: > 0 } && TimeZoneInfo.TryFindSystemTimeZoneById(id, out TimeZoneInfo? zone) ? zone : TimeZoneInfo.Utc;

    // SQL Server knows Windows names.
    // An unknown name falls back to UTC rather than failing the report (review).
    internal static string SqlZone(string? id) =>
        id is null or "UTC" or "Etc/UTC" ? "UTC"
            : TimeZoneInfo.TryConvertIanaIdToWindowsId(id, out string? windows) ? windows
            : TimeZoneInfo.TryConvertWindowsIdToIanaId(id, out string? _) ? id
            : "UTC";

    // Raw counts in a time zone; a zone the database does not know is counted in UTC instead of failing (review).
    private async Task<List<T>> InZoneAsync<T>(string sql, List<SqlParameter> parameters, CancellationToken cancellationToken)
    {
        try
        {
            return await _context.Database.SqlQueryRaw<T>(sql, parameters.Select(Copy).ToArray()).ToListAsync(cancellationToken);
        }
        catch (SqlException exception) when (exception.Number == 9820)
        {
            List<SqlParameter> utc = parameters.Select(Copy).ToList();
            utc.Single(parameter => parameter.ParameterName == "@zone").Value = "UTC";
            return await _context.Database.SqlQueryRaw<T>(sql, utc.ToArray()).ToListAsync(cancellationToken);
        }
    }

    private static SqlParameter Copy(SqlParameter parameter) => new(parameter.ParameterName, parameter.Value);

    internal static Dictionary<Guid, TimeZoneInfo> Zones(OperationsReportQuery query) => query.Places.ToDictionary(place => place.Id, place => Zone(place.TimeZone));

    internal static DateOnly Local(DateTimeOffset utc, TimeZoneInfo zone) => DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(utc, zone).DateTime);

    internal static DateOnly Local(DateTimeOffset utc, Dictionary<Guid, TimeZoneInfo> zones, Guid warehouseId) =>
        Local(utc, zones.TryGetValue(warehouseId, out TimeZoneInfo? zone) ? zone : TimeZoneInfo.Utc);

    internal static decimal? Average(IEnumerable<double> values)
    {
        List<double> list = values.ToList();
        return list.Count == 0 ? null : Math.Round((decimal)list.Average(), 1);
    }

    // Count, average, median and 90th percentile of a list of days.
    internal static decimal?[] Spread(List<double> values)
    {
        if (values.Count == 0)
        {
            return new decimal?[] { 0, null, null, null };
        }

        List<double> sorted = values.OrderBy(value => value).ToList();
        double At(double share) => sorted[Math.Min(sorted.Count - 1, (int)Math.Ceiling(share * sorted.Count) - 1)];
        return new decimal?[] { sorted.Count, Math.Round((decimal)sorted.Average(), 1), Math.Round((decimal)At(0.5), 1), Math.Round((decimal)At(0.9), 1) };
    }

    // A row per cut (day, week or month) from the first to the last day, quiet ones included; totals add up.
    internal static OperationsReportTable OverTime(OperationsReportQuery query, int columns, Action<Action<DateOnly, int, decimal>> fill)
    {
        IReadOnlyList<DateOnly> cuts = OperationsReportPeriods.Cuts(query.From, query.To, query.Grain);
        Dictionary<DateOnly, decimal[]> values = cuts.ToDictionary(cut => cut, _ => new decimal[columns]);
        decimal[] totals = new decimal[columns];
        fill((day, column, value) =>
        {
            if (column < 0 || !Between(query, day))
            {
                return;
            }

            values[OperationsReportPeriods.Start(day, query.Grain)][column] += value;
            totals[column] += value;
        });
        List<OperationsReportRow> rows = cuts.Select(cut => new OperationsReportRow(new OperationsReportLabel(Date: cut), values[cut].Select(value => (decimal?)value).ToList())).ToList();
        return new OperationsReportTable(rows, totals.Select(value => (decimal?)value).ToList());
    }

    // Rows by a key, largest first, at most 500 (totals over all).
    internal static OperationsReportTable Ranked(IEnumerable<(OperationsReportLabel Label, decimal?[] Values)> rows, int sortColumn, int columns)
    {
        List<(OperationsReportLabel Label, decimal?[] Values)> list = rows.OrderByDescending(row => row.Values[sortColumn] ?? 0m).ToList();
        decimal?[] totals = Enumerable.Range(0, columns).Select(column => (decimal?)list.Sum(row => row.Values[column] ?? 0m)).ToArray();
        return new OperationsReportTable(list.Take(OperationsReportTable.MaxRows).Select(row => new OperationsReportRow(row.Label, row.Values)).ToList(), totals)
        {
            Truncated = list.Count > OperationsReportTable.MaxRows
        };
    }

    internal static decimal?[] Hide(OperationsReportQuery query, decimal?[] values)
    {
        if (!query.Money)
        {
            for (int column = 0; column < values.Length; column++)
            {
                if (query.Report.Columns[column].IsMoney)
                {
                    values[column] = null;
                }
            }
        }

        return values;
    }

    private sealed record CodeDayCount(string Code, DateTime Day, int Count);

    private sealed record LocationDayMinutes(string Location, DateTime Day, int Minutes);
}
