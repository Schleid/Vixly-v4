using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Supplies;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Application.UseCases.Supplies.AdjustOperationsSupplyStock;
using Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyCount;
using Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyStock;
using Vixly.Operations.Application.UseCases.Supplies.ListOperationsSupplyMovements;
using Vixly.Operations.Application.UseCases.Supplies.OpenOperationsSupplyCount;
using Vixly.Operations.Application.UseCases.Supplies.ReceiveOperationsSupplies;
using Vixly.Operations.Application.UseCases.Supplies.ReceiveOperationsSupplyTransfer;
using Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupplyCount;
using Vixly.Operations.Application.UseCases.Supplies.SendOperationsSupplyTransfer;
using Vixly.Operations.Application.UseCases.Supplies.SetOperationsSupplyReorderLevel;
using Vixly.Operations.Domain.Supplies;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Supplies;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsSupplyStockEndpoints.cs
//
// PURPOSE:
// Supplies stock (Slice 5E step 2): the stock of a warehouse, receiving, adjusting, reorder levels, transfers, counts and the movements of a supply.
//
// RESPONSIBILITIES:
// - /staff/supplies/stock and its forms; /staff/supplies/{id}/movements
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); each stock form carries its key
// - The use cases check every permission (ops.supplies.stock, per warehouse)
// =========================================================
public static class OperationsSupplyStockEndpoints
{
    private const int MaxRows = 20;

    public static IEndpointRouteBuilder MapOperationsSupplyStockEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/supplies/stock", async (HttpContext http, Guid? warehouse, string? done, GetOperationsSupplyStockUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await StockAsync(http, warehouse, null, done is "received" or "adjusted" or "reorder" or "sent" or "arrived" ? text["ui.supplies.stock.done." + done].Value : null, get, brand, errors));

        staff.MapPost("/supplies/stock/receive", async (HttpContext http, GetOperationsSupplyStockUseCase get, ReceiveOperationsSuppliesUseCase receive,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            Guid warehouse = Id(form["warehouse"]);
            List<OperationsSupplyReceiptLine> lines = new();

            for (int row = 1; row <= MaxRows; row++)
            {
                if (!Guid.TryParse(form["line_supply_" + row], out Guid supply))
                {
                    continue;
                }

                decimal? cost = form["line_cost_" + row].ToString() is { Length: > 0 } typed ? Vixly.Operations.Application.Charges.OperationsPriceText.Number(typed) ?? -1m : null;
                lines.Add(new OperationsSupplyReceiptLine(supply, Count(form["line_qty_" + row]), cost));
            }

            ReceiveOperationsSuppliesResponse response = await receive.ExecuteAsync(OperationsCurrentUser.Context(http), new ReceiveOperationsSuppliesRequest
            {
                WarehouseId = warehouse, Lines = lines, Supplier = form["supplier"], Reference = form["reference"], Key = Id(form["key"])
            }, http.RequestAborted);
            return response.Succeeded ? Done(warehouse, "received") : await StockAsync(http, warehouse, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/supplies/stock/adjust", async (HttpContext http, GetOperationsSupplyStockUseCase get, AdjustOperationsSupplyStockUseCase adjust,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            Guid warehouse = Id(form["warehouse"]);
            AdjustOperationsSupplyStockResponse response = await adjust.ExecuteAsync(OperationsCurrentUser.Context(http), new AdjustOperationsSupplyStockRequest
            {
                WarehouseId = warehouse,
                SupplyId = Id(form["supply"]),
                Quantity = int.TryParse(form["quantity"], System.Globalization.NumberStyles.AllowLeadingSign, System.Globalization.CultureInfo.InvariantCulture, out int quantity) ? quantity : 0,
                Reason = Enum.TryParse(form["reason"], out OperationsSupplyAdjustReason reason) ? reason : 0,
                Note = form["note"],
                Key = Id(form["key"])
            }, http.RequestAborted);
            return response.Succeeded ? Done(warehouse, "adjusted") : await StockAsync(http, warehouse, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/supplies/stock/reorder", async (HttpContext http, GetOperationsSupplyStockUseCase get, SetOperationsSupplyReorderLevelUseCase reorder,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            Guid warehouse = Id(form["warehouse"]);
            string typed = form["level"].ToString().Trim();
            SetOperationsSupplyReorderLevelResponse response = await reorder.ExecuteAsync(OperationsCurrentUser.Context(http), new SetOperationsSupplyReorderLevelRequest
            {
                WarehouseId = warehouse, SupplyId = Id(form["supply"]), Level = typed.Length == 0 ? null : Count(typed)
            }, http.RequestAborted);
            return response.Succeeded ? Done(warehouse, "reorder") : await StockAsync(http, warehouse, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/supplies/stock/transfers", async (HttpContext http, GetOperationsSupplyStockUseCase get, SendOperationsSupplyTransferUseCase send,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            Guid warehouse = Id(form["warehouse"]);
            List<OperationsSupplyLine> lines = new();

            for (int row = 1; row <= MaxRows; row++)
            {
                if (Guid.TryParse(form["line_supply_" + row], out Guid supply))
                {
                    lines.Add(new OperationsSupplyLine(supply, Count(form["line_qty_" + row])));
                }
            }

            SendOperationsSupplyTransferResponse response = await send.ExecuteAsync(OperationsCurrentUser.Context(http), new SendOperationsSupplyTransferRequest
            {
                FromWarehouseId = warehouse, ToWarehouseId = Id(form["to"]), Lines = lines, Note = form["note"], Key = Id(form["key"])
            }, http.RequestAborted);
            return response.Succeeded ? Done(warehouse, "sent") : await StockAsync(http, warehouse, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/supplies/stock/transfers/{transferId:guid}/receive", async (HttpContext http, Guid transferId, GetOperationsSupplyStockUseCase get,
            ReceiveOperationsSupplyTransferUseCase receive, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ReceiveOperationsSupplyTransferResponse response = await receive.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ReceiveOperationsSupplyTransferRequest { TransferId = transferId }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/supplies/stock?done=arrived") : await StockAsync(http, null, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/supplies/stock/counts", async (HttpContext http, GetOperationsSupplyStockUseCase get, OpenOperationsSupplyCountUseCase open,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            Guid warehouse = Id(form["warehouse"]);
            OpenOperationsSupplyCountResponse response = await open.ExecuteAsync(OperationsCurrentUser.Context(http), new OpenOperationsSupplyCountRequest
            {
                WarehouseId = warehouse,
                Category = Enum.TryParse(form["category"], out OperationsSupplyCategory category) && Enum.IsDefined(category) ? category : null
            }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/supplies/stock/counts/" + response.CountId) : await StockAsync(http, warehouse, response.Error, null, get, brand, errors);
        });

        staff.MapGet("/supplies/stock/counts/{countId:guid}", async (HttpContext http, Guid countId, string? done, GetOperationsSupplyCountUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await CountAsync(http, countId, null, done == "saved" ? text["ui.supplies.stock.done.count_saved"].Value
                : done == "closed" ? text["ui.supplies.stock.done.count_closed"].Value : null, get, brand, errors));

        staff.MapPost("/supplies/stock/counts/{countId:guid}", async (HttpContext http, Guid countId, GetOperationsSupplyCountUseCase get, SaveOperationsSupplyCountUseCase save,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            Dictionary<Guid, int?> counted = new();

            foreach (string key in form.Keys.Where(key => key.StartsWith("count_", StringComparison.Ordinal)).Take(Vixly.Operations.Domain.Supplies.OperationsSupplyLimits.MaxSupplies))
            {
                if (Guid.TryParse(key[6..], out Guid supply))
                {
                    string typed = form[key].ToString().Trim();
                    counted[supply] = typed.Length == 0 ? null : Count(typed);
                }
            }

            string action = form["action"].ToString();
            SaveOperationsSupplyCountResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsSupplyCountRequest
            {
                CountId = countId, Counted = counted, Close = action == "close", Cancel = action == "cancel"
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/supplies/stock/counts/" + countId + (action is "close" or "cancel" ? "?done=closed" : "?done=saved"))
                : await CountAsync(http, countId, response.Error, null, get, brand, errors);
        });

        staff.MapGet("/supplies/{supplyId:guid}/movements", async (HttpContext http, Guid supplyId, Guid? warehouse, ListOperationsSupplyMovementsUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, Vixly.Operations.Application.Ports.IOperationsSettingsRepository settings) =>
        {
            ListOperationsSupplyMovementsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ListOperationsSupplyMovementsRequest { SupplyId = supplyId, WarehouseId = warehouse ?? Guid.Empty }, http.RequestAborted);
            return OperationsStaffResults.Page<SupplyMovementsPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                Supply = response.Succeeded ? response.Supply : null,
                Warehouse = response.Succeeded ? response.Warehouse : null,
                Stock = response.Stock,
                Movements = response.Movements,
                Currency = (await settings.FindAsync(http.RequestAborted))?.CurrencyCode ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.DefaultCurrency,
                Error = errors.Describe(response.Error)
            });
        });

        return endpoints;
    }

    private static Guid Id(string? typed) => Guid.TryParse(typed, out Guid id) ? id : Guid.Empty;

    // A whole number as typed; anything else is 0, which the rules refuse.
    private static int Count(string? typed) =>
        int.TryParse(typed?.Trim(), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int value) ? value : 0;

    private static IResult Done(Guid warehouse, string done) => Results.Redirect("/staff/supplies/stock?warehouse=" + warehouse + "&done=" + done);

    private static async Task<IResult> StockAsync(HttpContext http, Guid? warehouse, Error? error, string? info, GetOperationsSupplyStockUseCase get,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsSupplyStockResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSupplyStockRequest { WarehouseId = warehouse }, http.RequestAborted);
        return OperationsStaffResults.Page<SupplyStockPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Warehouses = response.Warehouses,
            Warehouse = response.Succeeded ? response.Warehouse : null,
            AllWarehouses = response.AllWarehouses,
            Supplies = response.Supplies,
            Stock = response.Stock,
            Transfers = response.Transfers,
            OpenCount = response.OpenCount,
            CountsStock = !response.Succeeded || response.CountsStock,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> CountAsync(HttpContext http, Guid countId, Error? error, string? info, GetOperationsSupplyCountUseCase get,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsSupplyCountResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSupplyCountRequest { CountId = countId }, http.RequestAborted);
        return OperationsStaffResults.Page<SupplyCountPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Count = response.Succeeded ? response.Count : null,
            WarehouseName = response.Succeeded ? response.Warehouse.Code + " · " + response.Warehouse.Name : string.Empty,
            Supplies = response.Supplies,
            Stock = response.Stock,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
