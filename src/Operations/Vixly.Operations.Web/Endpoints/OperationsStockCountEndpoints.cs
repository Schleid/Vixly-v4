using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Holds.CloseOperationsStockCount;
using Vixly.Operations.Application.UseCases.Holds.GetOperationsStockCount;
using Vixly.Operations.Application.UseCases.Holds.ListOperationsStockCounts;
using Vixly.Operations.Application.UseCases.Holds.MarkOperationsStockCount;
using Vixly.Operations.Application.UseCases.Holds.StartOperationsStockCount;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Parcels;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsStockCountEndpoints.cs
//
// PURPOSE:
// The counts of the undeclared parcels area (Slice 4D step 3, design section 6).
//
// RESPONSIBILITIES:
// - /staff/parcels/counts: the counts, start one for a warehouse
// - /staff/parcels/counts/{id}: mark each parcel found (picked or scanned) or missing, then close
//
// IMPORTANT NOTES:
// - Forms with antiforgery and the form guard (ADR-0019); the use cases check the permission and the warehouse
// =========================================================
public static class OperationsStockCountEndpoints
{
    public static IEndpointRouteBuilder MapOperationsStockCountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff/parcels/counts").RequireAuthorization().RequireFormToken();

        staff.MapGet("/", async (HttpContext http, ListOperationsStockCountsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await ListAsync(http, null, list, brand, errors));

        staff.MapPost("/", async (HttpContext http, [FromForm] Guid? warehouse, StartOperationsStockCountUseCase start, ListOperationsStockCountsUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            StartOperationsStockCountResponse response = await start.ExecuteAsync(OperationsCurrentUser.Context(http),
                new StartOperationsStockCountRequest { WarehouseId = warehouse ?? Guid.Empty }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/parcels/counts/" + response.CountId) : await ListAsync(http, response.Error, list, brand, errors);
        });

        staff.MapGet("/{countId:guid}", async (HttpContext http, Guid countId, string? done, GetOperationsStockCountUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await CountAsync(http, countId, null, done is "marked" or "closed" ? text["ui.counts.done." + done].Value : null, get, brand, errors));

        staff.MapPost("/{countId:guid}/mark", async (HttpContext http, Guid countId, [FromForm] Guid? parcel, [FromForm] string? code, [FromForm] string? result,
            [FromForm] string? note, MarkOperationsStockCountUseCase mark, GetOperationsStockCountUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            MarkOperationsStockCountResponse response = await mark.ExecuteAsync(OperationsCurrentUser.Context(http), new MarkOperationsStockCountRequest
            {
                CountId = countId,
                ParcelId = parcel,
                Code = code,
                Result = result == "missing" ? OperationsCountResult.Missing : OperationsCountResult.Found,
                Note = note
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/parcels/counts/" + countId + "?done=marked") : await CountAsync(http, countId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/{countId:guid}/close", async (HttpContext http, Guid countId, CloseOperationsStockCountUseCase close, GetOperationsStockCountUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            CloseOperationsStockCountResponse response = await close.ExecuteAsync(OperationsCurrentUser.Context(http), new CloseOperationsStockCountRequest { CountId = countId },
                http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/parcels/counts/" + countId + "?done=closed") : await CountAsync(http, countId, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

    private static async Task<IResult> ListAsync(HttpContext http, Error? error, ListOperationsStockCountsUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        ListOperationsStockCountsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsStockCountsRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<StockCountsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Counts = response.Counts,
            Warehouses = response.Warehouses,
            Error = errors.Describe(error ?? response.Error)
        });
    }

    private static async Task<IResult> CountAsync(HttpContext http, Guid countId, Error? error, string? info, GetOperationsStockCountUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsStockCountResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsStockCountRequest { CountId = countId }, http.RequestAborted);

        if (response.Count is not { } count)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<StockCountPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Count = count,
            WarehouseCode = response.Warehouse?.Code,
            CanClose = response.CanClose,
            IsManager = response.IsManager,
            Error = errors.Describe(error),
            Info = info
        });
    }
}
