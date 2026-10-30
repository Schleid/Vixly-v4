using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupply;
using Vixly.Operations.Application.UseCases.Supplies.GetOperationsSupplyLabels;
using Vixly.Operations.Application.UseCases.Supplies.ListOperationsSupplies;
using Vixly.Operations.Application.UseCases.Supplies.RemoveOperationsSupplyPrice;
using Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupply;
using Vixly.Operations.Application.UseCases.Supplies.SaveOperationsSupplySettings;
using Vixly.Operations.Application.UseCases.Supplies.ScheduleOperationsSupplyPrice;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Supplies;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Supplies;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsSupplyEndpoints.cs
//
// PURPOSE:
// Prices → Supplies (Slice 5E): the catalogue, a supply, its prices and its shelf labels.
//
// RESPONSIBILITIES:
// - /staff/supplies and its forms
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019)
// - The use cases check every permission
// =========================================================
public static class OperationsSupplyEndpoints
{
    public static IEndpointRouteBuilder MapOperationsSupplyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/supplies", async (HttpContext http, string? search, string? category, string? done, ListOperationsSuppliesUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await ListAsync(http, search, category, null, new OperationsSupplyForm(), done == "saved" ? text["ui.supplies.done.saved"].Value : null, list, brand, errors));

        staff.MapPost("/supplies", async (HttpContext http, SaveOperationsSupplyUseCase save, ListOperationsSuppliesUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OperationsSupplyForm form = OperationsSupplyForm.Read(http.Request.Form);
            (DateOnly? day, bool dayRead) = form.Day;

            if (!dayRead || form.BreakValues() is not { } breaks || form.WarehouseValues() is not { } places)
            {
                return await ListAsync(http, null, null, !dayRead ? OperationsChargeErrors.EffectiveInvalid : OperationsSupplyErrors.PriceInvalid, form, null, list, brand, errors);
            }

            SaveOperationsSupplyResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), Request(form, null, breaks, places, day), http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/supplies/" + response.SupplyId + "?done=saved")
                : await ListAsync(http, null, null, response.Error, form, null, list, brand, errors);
        });

        // Slice 5E step 2: count the stock or not, and the counter's rule when an item has none left.
        staff.MapPost("/supplies/settings", async (HttpContext http, SaveOperationsSupplySettingsUseCase save, ListOperationsSuppliesUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
        {
            SaveOperationsSupplySettingsResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsSupplySettingsRequest
            {
                CountStock = http.Request.Form["count"] == "true",
                StockOut = Enum.TryParse(http.Request.Form["stock_out"], out OperationsSupplyStockOut chosen) ? chosen : 0,
                DiscountsAllowed = http.Request.Form["discounts"] == "true",
                DiscountLinePercent = Percent(http.Request.Form["discount_line"]),
                DiscountOrderPercent = Percent(http.Request.Form["discount_order"]),
                DiscountReasonRequired = http.Request.Form["discount_reason"] == "true"
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/supplies?done=saved")
                : await ListAsync(http, null, null, response.Error, new OperationsSupplyForm(), null, list, brand, errors);
        });

        staff.MapGet("/supplies/{supplyId:guid}", async (HttpContext http, Guid supplyId, string? done, GetOperationsSupplyUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await SupplyAsync(http, supplyId, null, null, null, done switch
            {
                "saved" => text["ui.supplies.done.saved"].Value,
                "price" => text["ui.supplies.done.price"].Value,
                "removed" => text["ui.supplies.done.removed"].Value,
                _ => null
            }, get, brand, errors));

        staff.MapPost("/supplies/{supplyId:guid}", async (HttpContext http, Guid supplyId, SaveOperationsSupplyUseCase save, GetOperationsSupplyUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OperationsSupplyForm form = OperationsSupplyForm.Read(http.Request.Form);
            SaveOperationsSupplyResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), Request(form, supplyId, null, null, null), http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/supplies/" + supplyId + "?done=saved")
                : await SupplyAsync(http, supplyId, response.Error, form, null, null, get, brand, errors);
        });

        staff.MapPost("/supplies/{supplyId:guid}/prices", async (HttpContext http, Guid supplyId, ScheduleOperationsSupplyPriceUseCase schedule, GetOperationsSupplyUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OperationsSupplyForm form = OperationsSupplyForm.Read(http.Request.Form);
            (DateOnly? day, bool dayRead) = form.Day;

            if (!dayRead || form.BreakValues() is not { } breaks || form.WarehouseValues() is not { } places)
            {
                return await SupplyAsync(http, supplyId, !dayRead ? OperationsChargeErrors.EffectiveInvalid : OperationsSupplyErrors.PriceInvalid, null, form, null, get, brand, errors);
            }

            ScheduleOperationsSupplyPriceResponse response = await schedule.ExecuteAsync(OperationsCurrentUser.Context(http), new ScheduleOperationsSupplyPriceRequest
            {
                SupplyId = supplyId,
                Price = form.PriceValue ?? -1m,
                Breaks = breaks,
                WarehousePrices = places,
                EffectiveFrom = day
            }, http.RequestAborted);

            return response.Succeeded
                ? Results.Redirect("/staff/supplies/" + supplyId + "?done=price")
                : await SupplyAsync(http, supplyId, response.Error, null, form, null, get, brand, errors);
        });

        staff.MapPost("/supplies/{supplyId:guid}/prices/{priceId:guid}/remove", async (HttpContext http, Guid supplyId, Guid priceId, RemoveOperationsSupplyPriceUseCase remove,
            GetOperationsSupplyUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            RemoveOperationsSupplyPriceResponse response = await remove.ExecuteAsync(OperationsCurrentUser.Context(http),
                new RemoveOperationsSupplyPriceRequest { SupplyId = supplyId, PriceId = priceId }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/supplies/" + supplyId + "?done=removed")
                : await SupplyAsync(http, supplyId, response.Error, null, null, null, get, brand, errors);
        });

        staff.MapGet("/supplies/{supplyId:guid}/labels", async (HttpContext http, Guid supplyId, int? count, GetOperationsSupplyLabelsUseCase labels) =>
        {
            GetOperationsSupplyLabelsResponse response = await labels.ExecuteAsync(OperationsCurrentUser.Context(http),
                new GetOperationsSupplyLabelsRequest { SupplyId = supplyId, Count = count ?? 30 }, http.RequestAborted);
            return response.Succeeded ? OperationsStaffResults.File(http, response.Content, "application/pdf", response.FileName) : Results.NotFound();
        });

        return endpoints;
    }

    // A percentage as typed with a dot or a comma; anything else is refused by the rules (-1).
    private static decimal Percent(string? typed) =>
        string.IsNullOrWhiteSpace(typed) ? 0m : Vixly.Operations.Application.Charges.OperationsPriceText.Number(typed.Trim().Replace(',', '.')) ?? -1m;

    // A new supply carries its first price; a change carries only the details and barcodes.
    private static SaveOperationsSupplyRequest Request(OperationsSupplyForm form, Guid? supplyId, IReadOnlyList<OperationsSupplyBreak>? breaks,
        IReadOnlyList<OperationsSupplyWarehousePrice>? places, DateOnly? day) => new()
    {
        Price = supplyId is null ? form.PriceValue ?? -1m : null,
        Breaks = breaks ?? Array.Empty<OperationsSupplyBreak>(),
        WarehousePrices = places ?? Array.Empty<OperationsSupplyWarehousePrice>(),
        EffectiveFrom = day,
        SupplyId = supplyId,
        Code = form.Code,
        NameEn = form.NameEn,
        NameFr = form.NameFr,
        NameEs = form.NameEs,
        Unit = form.Unit,
        Category = form.CategoryValue,
        Taxed = form.Taxed,
        Active = form.Active,
        Barcodes = form.BarcodeList
    };

    private static async Task<IResult> ListAsync(HttpContext http, string? search, string? category, Error? error, OperationsSupplyForm entered, string? info,
        ListOperationsSuppliesUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        ListOperationsSuppliesResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsSuppliesRequest(), http.RequestAborted);
        return OperationsStaffResults.Page<SuppliesPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Supplies = response.Supplies,
            Warehouses = response.Warehouses,
            Currency = response.CurrencyCode,
            UtcNow = response.UtcNow,
            Search = search is { Length: <= 80 } ? search : null,
            Category = Enum.TryParse(category, out OperationsSupplyCategory chosen) && Enum.IsDefined(chosen) ? chosen : (OperationsSupplyCategory?)null,
            Entered = entered,
            CountsStock = response.CountsStock,
            StockOut = response.StockOut,
            Settings = response.Settings,
            Denied = !response.Succeeded,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }

    private static async Task<IResult> SupplyAsync(HttpContext http, Guid supplyId, Error? error, OperationsSupplyForm? details, OperationsSupplyForm? price, string? info,
        GetOperationsSupplyUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors)
    {
        GetOperationsSupplyResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSupplyRequest { SupplyId = supplyId }, http.RequestAborted);
        return OperationsStaffResults.Page<SupplyPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Supply = response.Succeeded ? response.Supply : null,
            Warehouses = response.Warehouses,
            Currency = response.CurrencyCode,
            UtcNow = response.UtcNow,
            DetailsEntered = details,
            PriceEntered = price ?? new OperationsSupplyForm(),
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
