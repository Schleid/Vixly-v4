using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Arrivals.FinishOperationsArrival;
using Vixly.Operations.Application.UseCases.Arrivals.GetOperationsArrival;
using Vixly.Operations.Application.UseCases.Arrivals.GetOperationsArrivals;
using Vixly.Operations.Application.UseCases.Arrivals.GetOperationsDiscrepancies;
using Vixly.Operations.Application.UseCases.Arrivals.OpenOperationsArrivalBundle;
using Vixly.Operations.Application.UseCases.Arrivals.RecordOperationsCustoms;
using Vixly.Operations.Application.UseCases.Arrivals.ResolveOperationsDiscrepancy;
using Vixly.Operations.Application.UseCases.Arrivals.ScanOperationsArrival;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Arrivals;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Arrivals;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Localization;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsArrivalEndpoints.cs
//
// PURPOSE:
// Destination receiving (Slice 7 step 1): the dispatches coming, one dispatch's receiving, customs, discrepancies.
//
// RESPONSIBILITIES:
// - /staff/arrivals, /staff/arrivals/{dispatchId} with scan, open, customs and finish; /staff/arrivals/discrepancies
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); the use cases check every permission
// - Every form names its warehouse; a dispatch not bound there is not found (R-11)
// =========================================================
public static partial class OperationsArrivalEndpoints
{
    public static IEndpointRouteBuilder MapOperationsArrivalEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/arrivals", async (HttpContext http, Guid? warehouse, GetOperationsArrivalsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            GetOperationsArrivalsResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsArrivalsRequest { WarehouseId = warehouse }, http.RequestAborted);
            return OperationsStaffResults.Page<ArrivalsPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                response.Warehouses,
                response.WarehouseId,
                response.Coming,
                response.Received,
                Error = errors.Describe(response.Error)
            });
        });

        staff.MapGet("/arrivals/discrepancies", async (HttpContext http, string? view, string? done, GetOperationsDiscrepanciesUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
            await DiscrepanciesAsync(http, view == "all", null, done == "1" ? texts["ui.arrivals.done.resolved"].Value : null, get, brand, errors));

        staff.MapPost("/arrivals/discrepancies/{id:guid}", async (HttpContext http, Guid id, [FromForm] string? outcome, [FromForm] string? note, ResolveOperationsDiscrepancyUseCase resolve,
            GetOperationsDiscrepanciesUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ResolveOperationsDiscrepancyResponse response = await resolve.ExecuteAsync(OperationsCurrentUser.Context(http), new ResolveOperationsDiscrepancyRequest
            {
                DiscrepancyId = id,
                Outcome = Enum.TryParse(outcome, false, out OperationsDiscrepancyStatus parsed) && Enum.IsDefined(parsed) ? parsed : (OperationsDiscrepancyStatus)0,
                Note = note,
                PortalSlug = OperationsStaffResults.PortalSlug(http)
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/arrivals/discrepancies?done=1") : await DiscrepanciesAsync(http, false, response.Error, null, get, brand, errors);
        });

        staff.MapGet("/arrivals/{dispatchId:guid}", async (HttpContext http, Guid dispatchId, Guid? warehouse, string? done, string? number, GetOperationsArrivalUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> texts) =>
            await ArrivalAsync(http, dispatchId, warehouse ?? Guid.Empty, null, Done(texts, done, number), get, brand, errors));

        staff.MapPost("/arrivals/{dispatchId:guid}/scan", async (HttpContext http, Guid dispatchId, [FromForm] Guid? warehouse, [FromForm] string? code, ScanOperationsArrivalUseCase scan,
            GetOperationsArrivalUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ScanOperationsArrivalResponse response = await scan.ExecuteAsync(OperationsCurrentUser.Context(http), new ScanOperationsArrivalRequest
            {
                DispatchId = dispatchId,
                WarehouseId = warehouse ?? Guid.Empty,
                Code = code,
                PortalSlug = OperationsStaffResults.PortalSlug(http)
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/arrivals/" + dispatchId + "?warehouse=" + warehouse + "&done=" + response.Outcome + (response.Ready > 0 ? "_ready" : string.Empty)
                    + "&number=" + Uri.EscapeDataString(response.Number))
                : await ArrivalAsync(http, dispatchId, warehouse ?? Guid.Empty, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/arrivals/{dispatchId:guid}/bundles/{bundleId:guid}/open", async (HttpContext http, Guid dispatchId, Guid bundleId, [FromForm] Guid? warehouse,
            OpenOperationsArrivalBundleUseCase open, GetOperationsArrivalUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OpenOperationsArrivalBundleResponse response = await open.ExecuteAsync(OperationsCurrentUser.Context(http), new OpenOperationsArrivalBundleRequest
            {
                DispatchId = dispatchId, WarehouseId = warehouse ?? Guid.Empty, BundleId = bundleId
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/arrivals/" + dispatchId + "?warehouse=" + warehouse + "&done=opened")
                : await ArrivalAsync(http, dispatchId, warehouse ?? Guid.Empty, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/arrivals/{dispatchId:guid}/customs", async (HttpContext http, Guid dispatchId, [FromForm] Guid? warehouse, [FromForm] string? step, [FromForm] string? bundle,
            [FromForm] string? message, [FromForm] string? key, RecordOperationsCustomsUseCase record, GetOperationsArrivalUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            RecordOperationsCustomsResponse response = await record.ExecuteAsync(OperationsCurrentUser.Context(http), new RecordOperationsCustomsRequest
            {
                DispatchId = dispatchId,
                WarehouseId = warehouse ?? Guid.Empty,
                BundleId = Guid.TryParse(bundle, out Guid bundleId) ? bundleId : null,
                Step = step,
                Message = message,
                RequestKey = Guid.TryParseExact(key, "N", out Guid formKey) ? formKey : null,
                PortalSlug = OperationsStaffResults.PortalSlug(http)
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/arrivals/" + dispatchId + "?warehouse=" + warehouse + "&done=customs")
                : await ArrivalAsync(http, dispatchId, warehouse ?? Guid.Empty, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/arrivals/{dispatchId:guid}/finish", async (HttpContext http, Guid dispatchId, [FromForm] Guid? warehouse, FinishOperationsArrivalUseCase finish,
            GetOperationsArrivalUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            FinishOperationsArrivalResponse response = await finish.ExecuteAsync(OperationsCurrentUser.Context(http), new FinishOperationsArrivalRequest
            {
                DispatchId = dispatchId, WarehouseId = warehouse ?? Guid.Empty
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/arrivals/" + dispatchId + "?warehouse=" + warehouse + "&done=" + (response.Missing > 0 ? "finished_missing" : "finished"))
                : await ArrivalAsync(http, dispatchId, warehouse ?? Guid.Empty, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

    // The words after a redirect; only known outcomes, and only a number's shape (letters, digits, dashes), as text (review).
    private static string? Done(IStringLocalizer<OperationsText> texts, string? done, string? number) =>
        done is "bundle" or "bundle_again" or "extra_bundle" or "parcel" or "parcel_again" or "extra_parcel" or "parcel_ready" or "parcel_again_ready" or "opened" or "customs"
            or "finished" or "finished_missing"
            ? VixlyText.Format(texts, "ui.arrivals.done." + done, "number", number is not null && NumberShape().IsMatch(number) ? number : string.Empty)
            : null;

    [System.Text.RegularExpressions.GeneratedRegex("^[A-Za-z0-9-]{1,40}$")]
    private static partial System.Text.RegularExpressions.Regex NumberShape();

    private static async Task<IResult> ArrivalAsync(HttpContext http, Guid dispatchId, Guid warehouseId, Error? error, string? info, GetOperationsArrivalUseCase get,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsArrivalResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsArrivalRequest { DispatchId = dispatchId, WarehouseId = warehouseId },
            http.RequestAborted);

        if (!response.Succeeded && error is null)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<ArrivalPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            DispatchId = dispatchId,
            response.Number,
            response.FromCode,
            response.Status,
            response.Onward,
            response.ReceivedUtc,
            response.Here,
            response.CanCustoms,
            response.CanReceive,
            response.Bundles,
            response.Discrepancies,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> DiscrepanciesAsync(HttpContext http, bool all, Error? error, string? info, GetOperationsDiscrepanciesUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsDiscrepanciesResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsDiscrepanciesRequest { OpenOnly = !all }, http.RequestAborted);
        return OperationsStaffResults.Page<DiscrepanciesPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Items,
            All = all,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
