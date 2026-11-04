using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Counter.AddOperationsCounterSupply;
using Vixly.Operations.Application.UseCases.Counter.CancelOperationsSupplyOrder;
using Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterSupplyLine;
using Vixly.Operations.Application.UseCases.Counter.ConfirmOperationsSupplyOrder;
using Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterCustomer;
using Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterShipment;
using Vixly.Operations.Application.UseCases.Counter.GetOperationsSupplyOrder;
using Vixly.Operations.Application.UseCases.Counter.StartOperationsSupplyOrder;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Counter;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsCounterSupplyEndpoints.cs
//
// PURPOSE:
// Supplies at the counter (Slice 5E step 3): added to a shipment being processed, ordered on their own, changed and discounted, confirmed or cancelled.
//
// RESPONSIBILITIES:
// - /staff/counter/shipments/{id}/supplies, /staff/counter/customers/{id}/orders, /staff/counter/orders/{id} and its forms, /staff/counter/sales/{id}/lines/{line}
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019); each form carries its key
// - The use cases check every permission (ops.counter.serve, per warehouse)
// - A discount approval takes another person's authenticator code: rate limited as a second factor
// =========================================================
public static class OperationsCounterSupplyEndpoints
{
    public static IEndpointRouteBuilder MapOperationsCounterSupplyEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff/counter").RequireAuthorization().RequireFormToken();

        // A scan or a name on the shipment being processed.
        staff.MapPost("/shipments/{shipmentId:guid}/supplies", async (HttpContext http, Guid shipmentId, AddOperationsCounterSupplyUseCase add, GetOperationsCounterShipmentUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            string here = "/staff/counter/shipments/" + shipmentId;
            AddOperationsCounterSupplyResponse response = await add.ExecuteAsync(OperationsCurrentUser.Context(http), Request(form, shipmentId, null), http.RequestAborted);

            return !response.Succeeded ? await OperationsCounterEndpoints.ShipmentAsync(http, shipmentId, response.Error, null, null, get, brand, errors)
                : response.Choices.Count > 0 ? await ChoicesAsync(http, form, response, here + "/supplies", here, brand)
                : Results.Redirect(here + (response.LowStock ? "?done=supply_short" : "?done=supply"));
        });

        staff.MapPost("/customers/{customerId:guid}/orders", async (HttpContext http, Guid customerId, StartOperationsSupplyOrderUseCase start, GetOperationsCounterCustomerUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            StartOperationsSupplyOrderResponse response = await start.ExecuteAsync(OperationsCurrentUser.Context(http), new StartOperationsSupplyOrderRequest
            {
                CustomerId = customerId,
                WarehouseId = Guid.TryParse(form["warehouse"], out Guid warehouse) ? warehouse : null
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/counter/orders/" + response.SaleId)
                : await OperationsCounterEndpoints.CustomerAsync(http, customerId, response.Error, null, null, get, brand, errors);
        });

        staff.MapGet("/orders/{saleId:guid}", async (HttpContext http, Guid saleId, string? done, GetOperationsSupplyOrderUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await OrderAsync(http, saleId, null, done is "supply" or "supply_short" or "line" or "confirmed" or "cancelled" ? text["ui.counter.supplies.done." + done].Value : null, get, brand, errors));

        staff.MapPost("/orders/{saleId:guid}/supplies", async (HttpContext http, Guid saleId, AddOperationsCounterSupplyUseCase add, GetOperationsSupplyOrderUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            string here = "/staff/counter/orders/" + saleId;
            AddOperationsCounterSupplyResponse response = await add.ExecuteAsync(OperationsCurrentUser.Context(http), Request(form, null, saleId), http.RequestAborted);

            return !response.Succeeded ? await OrderAsync(http, saleId, response.Error, null, get, brand, errors)
                : response.Choices.Count > 0 ? await ChoicesAsync(http, form, response, here + "/supplies", here, brand)
                : Results.Redirect(here + (response.LowStock ? "?done=supply_short" : "?done=supply"));
        });

        staff.MapPost("/sales/{saleId:guid}/lines/{lineId:guid}", async (HttpContext http, Guid saleId, Guid lineId, ChangeOperationsCounterSupplyLineUseCase change,
            GetOperationsSupplyOrderUseCase order, GetOperationsCounterShipmentUseCase shipment, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            Guid? shipmentId = Guid.TryParse(form["shipment"], out Guid id) ? id : null;
            string typedDiscount = form["discount"].ToString().Trim();
            decimal? discount = typedDiscount.Length == 0 ? 0m : Vixly.Operations.Application.Charges.OperationsPriceText.Number(typedDiscount.Replace(',', '.'));
            ChangeOperationsCounterSupplyLineResponse response = await change.ExecuteAsync(OperationsCurrentUser.Context(http), new ChangeOperationsCounterSupplyLineRequest
            {
                SaleId = saleId,
                LineId = lineId,
                Quantity = form["remove"] == "true" ? 0 : int.TryParse(form["quantity"], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int quantity) ? quantity : -1,
                DiscountPercent = discount ?? -1m,
                DiscountReason = form["reason"],
                ApproverEmail = form["approver_email"],
                ApproverCode = form["approver_code"],
                Key = Guid.TryParse(form["key"], out Guid key) ? key : Guid.Empty
            }, http.RequestAborted);

            string back = shipmentId is { } owner ? "/staff/counter/shipments/" + owner : "/staff/counter/orders/" + saleId;
            return response.Succeeded ? Results.Redirect(back + (response.LowStock ? "?done=supply_short" : "?done=line"))
                : shipmentId is { } failed ? await OperationsCounterEndpoints.ShipmentAsync(http, failed, response.Error, null, null, shipment, brand, errors)
                : await OrderAsync(http, saleId, response.Error, null, order, brand, errors);
        }).RequireRateLimiting(OperationsRateLimits.SecondFactor);

        staff.MapPost("/orders/{saleId:guid}/confirm", async (HttpContext http, Guid saleId, ConfirmOperationsSupplyOrderUseCase confirm, GetOperationsSupplyOrderUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            ConfirmOperationsSupplyOrderResponse response = await confirm.ExecuteAsync(OperationsCurrentUser.Context(http), new ConfirmOperationsSupplyOrderRequest { SaleId = saleId }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/counter/orders/" + saleId + "?done=confirmed") : await OrderAsync(http, saleId, response.Error, null, get, brand, errors);
        });

        staff.MapPost("/orders/{saleId:guid}/cancel", async (HttpContext http, Guid saleId, CancelOperationsSupplyOrderUseCase cancel, GetOperationsSupplyOrderUseCase get,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            CancelOperationsSupplyOrderResponse response = await cancel.ExecuteAsync(OperationsCurrentUser.Context(http), new CancelOperationsSupplyOrderRequest { SaleId = saleId }, http.RequestAborted);
            return response.Succeeded ? Results.Redirect("/staff/counter/orders/" + saleId + "?done=cancelled") : await OrderAsync(http, saleId, response.Error, null, get, brand, errors);
        });

        return endpoints;
    }

    private static AddOperationsCounterSupplyRequest Request(IFormCollection form, Guid? shipmentId, Guid? saleId) => new()
    {
        ShipmentId = shipmentId,
        SaleId = saleId,
        Scan = form["scan"],
        SupplyId = Guid.TryParse(form["supply"], out Guid supply) ? supply : null,
        Key = Guid.TryParse(form["key"], out Guid key) ? key : Guid.Empty
    };

    private static async Task<IResult> ChoicesAsync(HttpContext http, IFormCollection form, AddOperationsCounterSupplyResponse response, string add, string back,
        GetOperationsSignInBrandUseCase brand) =>
        OperationsStaffResults.Page<CounterSupplyChoices>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Text = form["scan"].ToString(),
            Choices = response.Choices,
            AddAction = add,
            Back = back,
            Currency = (await http.RequestServices.GetRequiredService<Vixly.Operations.Application.Ports.IOperationsSettingsRepository>().FindAsync(http.RequestAborted))?.CurrencyCode
                ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.DefaultCurrency,
            UtcNow = DateTimeOffset.UtcNow
        });

    private static async Task<IResult> OrderAsync(HttpContext http, Guid saleId, Error? error, string? info, GetOperationsSupplyOrderUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsSupplyOrderResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsSupplyOrderRequest { SaleId = saleId }, http.RequestAborted);
        return OperationsStaffResults.Page<CounterSupplyOrderPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Sale = response.Succeeded ? response.Sale : null,
            Customer = response.Succeeded ? response.Customer : null,
            Warehouse = response.Succeeded ? response.Warehouse.Code + " · " + response.Warehouse.Name : string.Empty,
            DiscountsAllowed = response.DiscountsAllowed,
            LinePercent = response.LinePercent,
            OrderPercent = response.OrderPercent,
            Invoice = response.Succeeded ? response.Invoice : null,
            Error = errors.Describe(error ?? response.Error),
            Info = info
        });
    }
}
