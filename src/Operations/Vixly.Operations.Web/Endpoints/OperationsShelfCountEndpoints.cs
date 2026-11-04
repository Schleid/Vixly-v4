using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.UseCases.Receiving.FinishOperationsShelfCount;
using Vixly.Operations.Application.UseCases.Receiving.GetOperationsShelfCounts;
using Vixly.Operations.Application.UseCases.Receiving.PutAwayOperationsParcel;
using Vixly.Operations.Application.UseCases.Receiving.ScanOperationsShelfCount;
using Vixly.Operations.Application.UseCases.Receiving.StartOperationsShelfCount;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Web.Components.Parcels;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsShelfCountEndpoints.cs
//
// PURPOSE:
// The basic shelf count (Slice 12D, owner H-7): scan a shelf, scan its parcels, see what is found, missing and extra.
//
// RESPONSIBILITIES:
// - /staff/parcels/shelf-counts: the latest counts; start one by scanning the shelf
// - /staff/parcels/shelf-counts/{id}: scan the parcels, finish, and move an extra parcel here
//
// IMPORTANT NOTES:
// - Forms with antiforgery and the form guard (ADR-0019); the use cases check Receive at the warehouse
// - Each scan ends with Enter and submits, which suits keyboard-wedge scanners and the camera button alike
// =========================================================
public static class OperationsShelfCountEndpoints
{
    public static IEndpointRouteBuilder MapOperationsShelfCountEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder counts = endpoints.MapGroup("/staff/parcels/shelf-counts").RequireAuthorization().RequireFormToken();

        counts.MapGet("/", async (HttpContext http, GetOperationsShelfCountsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await ListAsync(http, null, get, brand, errors));

        counts.MapPost("/", async (HttpContext http, StartOperationsShelfCountUseCase start, GetOperationsShelfCountsUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            StartOperationsShelfCountResponse response = await start.ExecuteAsync(OperationsCurrentUser.Context(http), new StartOperationsShelfCountRequest
            {
                WarehouseId = Guid.TryParse(form["warehouse"].ToString(), out Guid warehouse) ? warehouse : null,
                ShelfCode = form["shelf"].ToString()
            }, http.RequestAborted);

            return response.Succeeded ? Results.Redirect("/staff/parcels/shelf-counts/" + response.CountId) : await ListAsync(http, response.Error, get, brand, errors);
        });

        counts.MapGet("/{countId:guid}", async (HttpContext http, Guid countId, string? done, GetOperationsShelfCountsUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
            await CountAsync(http, countId, null, done, get, brand, errors));

        counts.MapPost("/{countId:guid}/scan", async (HttpContext http, Guid countId, ScanOperationsShelfCountUseCase scan, GetOperationsShelfCountsUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            ScanOperationsShelfCountResponse response = await scan.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ScanOperationsShelfCountRequest { CountId = countId, Code = form["code"].ToString() }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/parcels/shelf-counts/" + countId + "?done=" + (response.Added ? "scanned" : "again"))
                : await CountAsync(http, countId, response.Error, null, get, brand, errors);
        });

        counts.MapPost("/{countId:guid}/finish", async (HttpContext http, Guid countId, FinishOperationsShelfCountUseCase finish, GetOperationsShelfCountsUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            FinishOperationsShelfCountResponse response = await finish.ExecuteAsync(OperationsCurrentUser.Context(http), new FinishOperationsShelfCountRequest { CountId = countId },
                http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/parcels/shelf-counts/" + countId + "?done=finished") : await CountAsync(http, countId, response.Error, null, get, brand, errors);
        });

        // An extra parcel found on the shelf: put away here, as the move station does (a put-away event with the shelf).
        counts.MapPost("/{countId:guid}/move", async (HttpContext http, Guid countId, PutAwayOperationsParcelUseCase putAway, GetOperationsShelfCountsUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            GetOperationsShelfCountsResponse count = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsShelfCountsRequest { CountId = countId }, http.RequestAborted);

            if (count.Counts.FirstOrDefault() is not { } view)
            {
                return Results.NotFound();
            }

            string parcel = form["parcel"].ToString();
            PutAwayOperationsParcelResponse response = await putAway.ExecuteAsync(OperationsCurrentUser.Context(http), new PutAwayOperationsParcelRequest
            {
                ParcelCode = parcel,
                ShelfCode = view.ShelfCode,
                CommandKey = "count-" + countId.ToString("N")[..12] + "-" + new string(parcel.Where(char.IsAsciiLetterOrDigit).Take(30).ToArray())
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/parcels/shelf-counts/" + countId + "?done=moved") : await CountAsync(http, countId, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

    private static async Task<IResult> ListAsync(HttpContext http, Error? error, GetOperationsShelfCountsUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsShelfCountsResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsShelfCountsRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<ShelfCountsPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            response.Counts,
            response.Warehouses,
            Error = errors.Describe(error ?? response.Error)
        });
    }

    private static async Task<IResult> CountAsync(HttpContext http, Guid countId, Error? error, string? done, GetOperationsShelfCountsUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsShelfCountsResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsShelfCountsRequest { CountId = countId }, http.RequestAborted);

        if (response.Counts.FirstOrDefault() is not { } count)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<ShelfCountPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Count = count,
            Done = done is "scanned" or "again" or "finished" or "moved" ? done : null,
            Error = errors.Describe(error)
        });
    }
}
