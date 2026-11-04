using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Reports;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Reports.RunOperationsReport;

// =========================================================
// FILE: RunOperationsReportUseCase.cs
//
// PURPOSE:
// Runs one report for its days, cut and warehouse (Slice 11).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The report right (finance for finance reports), over the warehouses chosen; company-wide reports need it company-wide
// - At most 13 months; compared with the period just before
// =========================================================
public sealed class RunOperationsReportUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsReportReader _reader;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public RunOperationsReportUseCase(
        OperationsAccess access,
        IOperationsReportReader reader,
        IOperationsLocationRepository locations,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RunOperationsReportResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RunOperationsReportRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (OperationsReportCatalog.Find(request.Key) is not { } report)
        {
            return RunOperationsReportResponse.Failure(OperationsReportErrors.NotFound);
        }

        string right = report.Finance ? OperationsPermissions.ReportsFinance : OperationsPermissions.ReportsOperations;
        // Given per warehouse or company-wide (owner, 2026-10-22): the warehouses it covers are worked out below.
        // A scheduled export runs for its recipient, with their rights now (Slice 11).
        if (await _access.LoadScheduledAsync(context, cancellationToken) is not { } access)
        {
            return RunOperationsReportResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (!access.Has(right))
        {
            return RunOperationsReportResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (report.CompanyWide && !access!.HasTenantWide(right))
        {
            return RunOperationsReportResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // Warehouse-scoped staff see only their warehouses (owner, 2026-10-22).
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        string companyZone = settings?.TimeZone ?? "UTC";
        IReadOnlyList<OperationsWarehouse> warehouses = await _locations.ListWarehousesAsync(cancellationToken);
        List<OperationsWarehouse> allowed = warehouses.Where(warehouse => access!.CoversWarehouse(right, warehouse.Id, warehouse.BranchId)).ToList();

        if (allowed.Count == 0)
        {
            return RunOperationsReportResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        List<OperationsWarehouse> chosen = allowed;

        // A company-wide report is never cut by warehouse (review).
        if (request.WarehouseId is { } warehouseId && !report.CompanyWide)
        {
            if (allowed.FirstOrDefault(warehouse => warehouse.Id == warehouseId) is not { } one)
            {
                return RunOperationsReportResponse.Failure(OperationsReportErrors.WarehouseNotAllowed);
            }

            chosen = new List<OperationsWarehouse> { one };
        }

        // Days in the company's time zone by default: the last 30, or today for a picture.
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        DateOnly today = DateOnly.FromDateTime(TimeZoneInfo.TryFindSystemTimeZoneById(companyZone, out TimeZoneInfo? zone)
            ? TimeZoneInfo.ConvertTime(utcNow, zone).DateTime : utcNow.UtcDateTime);
        DateOnly to = report.Now ? today : request.To ?? today;
        DateOnly from = report.AsOf ? to : request.From ?? to.AddDays(-29);

        if (!OperationsReportPeriods.Valid(from, to))
        {
            return RunOperationsReportResponse.Failure(OperationsReportErrors.PeriodInvalid);
        }

        OperationsReportDimension dimension = request.Dimension is { } asked && report.Dimensions.Contains(asked) ? asked
            : report.Dimensions.Count > 0 ? report.Dimensions[0] : OperationsReportDimension.Period;
        int days = Math.Clamp(request.Days, 7, 730);
        List<OperationsReportPlace> places = chosen.Select(warehouse => new OperationsReportPlace(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.TimeZone ?? companyZone)).ToList();

        // Money only for whoever holds the finance right over every warehouse in the report.
        bool money = chosen.All(warehouse => access!.CoversWarehouse(OperationsPermissions.ReportsFinance, warehouse.Id, warehouse.BranchId))
            && (!report.CompanyWide || access!.HasTenantWide(OperationsPermissions.ReportsFinance));
        OperationsReportQuery query = new(report, from, to, request.Grain, dimension, places, companyZone)
        {
            AllPlaces = chosen.Count == warehouses.Count,
            Money = money,
            Days = days,
            GraceHours = settings?.DispatchGraceHours ?? 0,
            UtcNow = utcNow
        };
        OperationsReportTable table = await _reader.RunAsync(query, cancellationToken);
        IReadOnlyList<decimal?>? previous = null;
        (DateOnly From, DateOnly To)? before = null;

        // Compared with the period just before (not for a picture of today).
        if (!report.AsOf)
        {
            before = OperationsReportPeriods.Before(from, to);
            previous = (await _reader.RunAsync(query with { From = before.Value.From, To = before.Value.To }, cancellationToken)).Totals;
        }

        return RunOperationsReportResponse.Success(report, table, previous, from, to, before?.From, before?.To, request.Grain, dimension, report.CompanyWide ? null : request.WarehouseId, days,
            allowed.Select(warehouse => new OperationsReportPlace(warehouse.Id, warehouse.Code, warehouse.Name, warehouse.TimeZone ?? companyZone)).ToList(), money,
            settings?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency, settings?.WeightUnit ?? OperationsWeightUnit.Kilogram);
    }
}
