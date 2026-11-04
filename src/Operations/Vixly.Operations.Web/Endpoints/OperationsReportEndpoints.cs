using System.Globalization;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Reports;
using Vixly.Operations.Application.UseCases.Reports.GetOperationsReportRules;
using Vixly.Operations.Application.UseCases.Reports.ListOperationsReportSchedules;
using Vixly.Operations.Application.UseCases.Reports.RemoveOperationsReportSchedule;
using Vixly.Operations.Application.UseCases.Reports.SaveOperationsReportSchedule;
using Vixly.Operations.Domain.Reports;
using Vixly.Operations.Application.UseCases.Reports.ListOperationsReports;
using Vixly.Operations.Application.UseCases.Reports.RunOperationsReport;
using Vixly.Operations.Application.UseCases.Reports.SaveOperationsReportRules;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Reports;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsReportEndpoints.cs
//
// PURPOSE:
// Reports (Slice 11): the menu, one report, its files, and Settings → Reports.
//
// RESPONSIBILITIES:
// - /staff/reports, /staff/reports/{key}, /staff/reports/{key}/export?format=csv|xlsx|pdf
// - /staff/settings/reports: the on-time rules
//
// IMPORTANT NOTES:
// - Filters are read from the address (GET); anything unreadable falls back to the defaults
// - Files are never cached (private, no-store)
// =========================================================
public static class OperationsReportEndpoints
{
    public static IEndpointRouteBuilder MapOperationsReportEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/reports", (HttpContext http, string? done, ListOperationsReportsUseCase list, ListOperationsReportSchedulesUseCase schedules, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) => MenuAsync(http, null, list, schedules, brand, errors));

        staff.MapGet("/reports/{key}", (HttpContext http, string key, string? done, RunOperationsReportUseCase run, ListOperationsReportSchedulesUseCase schedules,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            ReportAsync(http, key, null, done is null ? null : text["ui.reports.schedule.saved"].Value, run, schedules, brand, errors));

        staff.MapPost("/reports/{key}/schedule", async (HttpContext http, string key, SaveOperationsReportScheduleUseCase save, RunOperationsReportUseCase run,
            ListOperationsReportSchedulesUseCase schedules, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            SaveOperationsReportScheduleResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsReportScheduleRequest
            {
                Key = key,
                Frequency = Enum.TryParse(form["frequency"], false, out OperationsReportFrequency frequency) && Enum.IsDefined(frequency) ? frequency : 0,
                Format = Enum.TryParse(form["format"], false, out OperationsReportFormat format) && Enum.IsDefined(format) ? format : 0,
                Grain = Enum.TryParse(form["grain"], true, out OperationsReportGrain grain) && Enum.IsDefined(grain) ? grain : OperationsReportGrain.Day,
                Dimension = Enum.TryParse(form["by"], true, out OperationsReportDimension dimension) && Enum.IsDefined(dimension) ? dimension : null,
                WarehouseId = Guid.TryParse(form["warehouse"], out Guid warehouse) ? warehouse : null,
                Days = int.TryParse(form["days"], NumberStyles.None, CultureInfo.InvariantCulture, out int days) ? days : 90,
                Recipients = form["recipients"].Select(value => Guid.TryParse(value, out Guid id) ? id : Guid.Empty).ToList()
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/reports/" + Uri.EscapeDataString(key) + "?done=1")
                : await ReportAsync(http, key, response.Error, null, run, schedules, brand, errors);
        });

        staff.MapPost("/reports/schedules/{scheduleId:guid}/remove", async (HttpContext http, Guid scheduleId, RemoveOperationsReportScheduleUseCase remove, ListOperationsReportsUseCase list,
            ListOperationsReportSchedulesUseCase schedules, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            RemoveOperationsReportScheduleResponse response = await remove.ExecuteAsync(OperationsCurrentUser.Context(http),
                new RemoveOperationsReportScheduleRequest { ScheduleId = scheduleId }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/reports?done=1") : await MenuAsync(http, response.Error, list, schedules, brand, errors);
        });

        staff.MapGet("/reports/{key}/export", async (HttpContext http, string key, string? format, RunOperationsReportUseCase run, GetOperationsSignInBrandUseCase brand,
            IOperationsReportFiles files, IStringLocalizer<OperationsText> text) =>
        {
            RunOperationsReportResponse response = await run.ExecuteAsync(OperationsCurrentUser.Context(http), Request(http, key), http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.Forbid();
            }

            OperationsReportSheet sheet = OperationsReportWords.Sheet(text, response, await OperationsStaffResults.CompanyAsync(http, brand));
            string name = key + "-" + OperationsReportPeriods.Iso(response.From) + "-" + OperationsReportPeriods.Iso(response.To);
            return format switch
            {
                "xlsx" => OperationsStaffResults.File(http, files.Xlsx(sheet), "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", name + ".xlsx"),
                "pdf" => OperationsStaffResults.File(http, files.Pdf(sheet), "application/pdf", name + ".pdf"),
                _ => OperationsStaffResults.File(http, OperationsReportCsv.Write(sheet), "text/csv", name + ".csv")
            };
        });

        staff.MapGet("/settings/reports", async (HttpContext http, string? done, GetOperationsReportRulesUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors,
            IStringLocalizer<OperationsText> text) => await RulesAsync(http, null, done is null ? null : text["ui.staff.done.saved"].Value, get, brand, errors));

        staff.MapPost("/settings/reports", async (HttpContext http, SaveOperationsReportRulesUseCase save, GetOperationsReportRulesUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            Dictionary<string, int?> targets = new(StringComparer.Ordinal);

            foreach (string field in form.Keys.Where(field => field.StartsWith("ready_", StringComparison.Ordinal)))
            {
                targets[field["ready_".Length..]] = int.TryParse(form[field], NumberStyles.None, CultureInfo.InvariantCulture, out int days) ? days : null;
            }

            SaveOperationsReportRulesResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsReportRulesRequest
            {
                DispatchGraceHours = int.TryParse(form["graceHours"], NumberStyles.None, CultureInfo.InvariantCulture, out int hours) ? hours : -1,
                ReadyTargetDays = targets
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/settings/reports?done=1") : await RulesAsync(http, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

    private static async Task<IResult> MenuAsync(HttpContext http, Error? error, ListOperationsReportsUseCase list, ListOperationsReportSchedulesUseCase schedules,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        ListOperationsReportsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsReportsRequest(), http.RequestAborted);
        ListOperationsReportSchedulesResponse planned = await schedules.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsReportSchedulesRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<ReportsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Reports,
            response.SeesProfit,
            planned.Schedules,
            planned.Staff,
            Error = errors.Describe(error ?? response.Error)
        });
    }

    private static async Task<IResult> ReportAsync(HttpContext http, string key, Error? error, string? info, RunOperationsReportUseCase run, ListOperationsReportSchedulesUseCase schedules,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        RunOperationsReportResponse response = await run.ExecuteAsync(OperationsCurrentUser.Context(http), Request(http, key), http.RequestAborted);
        ListOperationsReportSchedulesResponse planned = await schedules.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsReportSchedulesRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<ReportPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Run = response,
            planned.Staff,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static RunOperationsReportRequest Request(HttpContext http, string key)
    {
        IQueryCollection query = http.Request.Query;
        return new RunOperationsReportRequest
        {
            Key = key,
            From = Day(query["from"]),
            To = Day(query["to"]),
            Grain = Enum.TryParse(query["grain"], true, out OperationsReportGrain grain) && Enum.IsDefined(grain) ? grain : OperationsReportGrain.Day,
            Dimension = Enum.TryParse(query["by"], true, out OperationsReportDimension dimension) && Enum.IsDefined(dimension) ? dimension : null,
            WarehouseId = Guid.TryParse(query["warehouse"], out Guid warehouse) ? warehouse : null,
            Days = int.TryParse(query["days"], NumberStyles.None, CultureInfo.InvariantCulture, out int days) ? days : 90
        };
    }

    private static DateOnly? Day(string? typed) =>
        DateOnly.TryParseExact(typed, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly day) ? day : null;

    private static async Task<IResult> RulesAsync(HttpContext http, Error? error, string? info, GetOperationsReportRulesUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsReportRulesResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsReportRulesRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<ReportRulesPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            GraceHours = response.DispatchGraceHours,
            response.Services,
            response.CanEdit,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
