using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.UseCases.Charges.ListOperationsQuotes;
using Vixly.Operations.Application.UseCases.Charges.PriceOperationsShipment;
using Vixly.Operations.Application.UseCases.Charges.SaveOperationsQuote;
using Vixly.Operations.Application.UseCases.Charges.WithdrawOperationsQuote;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Charges;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsQuotesEndpoints.cs
//
// PURPOSE:
// Quotes: price a shipment on a route, save a quote with a preferred price, withdraw one (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - /staff/quotes and its forms
//
// IMPORTANT NOTES:
// - Static forms with antiforgery and the form guard (ADR-0019)
// - The use cases check every permission
// =========================================================
public static class OperationsQuotesEndpoints
{
    public static IEndpointRouteBuilder MapOperationsQuotesEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff").RequireAuthorization().RequireFormToken();

        staff.MapGet("/quotes", async (HttpContext http, string? done, string? number, PriceOperationsShipmentUseCase price, ListOperationsQuotesUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await QuotesAsync(http, new OperationsQuoteForm(), null, done == "saved" && number is { Length: <= 20 } ? Vixly.Shared.Localization.VixlyText.Format(text, "ui.quotes.done.saved", "number", number) : null,
                price, list, brand, errors, rate: false));

        // The quotes list with statuses (owner, 2026-10-20).
        staff.MapGet("/quotes/list", async (HttpContext http, string? state, string? search, int? page, string? done, ListOperationsQuotesUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
        {
            string key = state is "open" or "expired" or "withdrawn" ? state : "all";
            ListOperationsQuotesResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsQuotesRequest
            {
                State = key switch
                {
                    "open" => OperationsQuoteState.Open,
                    "expired" => OperationsQuoteState.Expired,
                    "withdrawn" => OperationsQuoteState.Withdrawn,
                    _ => null
                },
                Search = search,
                Page = page is > 0 and < 10000 ? page.Value : 1
            }, http.RequestAborted);
            return OperationsStaffResults.Page<QuoteListPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                State = key,
                Search = search,
                Page = response.Page,
                PageSize = response.PageSize,
                TotalCount = response.TotalCount,
                Counts = response.Counts,
                Quotes = response.Quotes,
                UtcNow = response.UtcNow,
                Denied = !response.Succeeded,
                Error = errors.Describe(response.Error),
                Info = done == "withdrawn" ? text["ui.quotes.done.withdrawn"].Value : null
            });
        });

        // [FromForm] turns on the antiforgery check; the rows are read from the same form.
        staff.MapPost("/quotes/price", async (HttpContext http, [FromForm] string? destination, PriceOperationsShipmentUseCase price, ListOperationsQuotesUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
            await QuotesAsync(http, OperationsQuoteForm.Read(http.Request.Form), null, null, price, list, brand, errors, rate: true));

        staff.MapPost("/quotes", async (HttpContext http, [FromForm] string? destination, SaveOperationsQuoteUseCase save, PriceOperationsShipmentUseCase price, ListOperationsQuotesUseCase list,
            GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            OperationsQuoteForm form = OperationsQuoteForm.Read(http.Request.Form);
            decimal? preferred = form.Preferred is null ? null : OperationsPriceText.Number(form.Preferred) ?? -1m;
            int days = form.Days is null ? OperationsQuote.DefaultValidDays : OperationsPriceText.Number(form.Days) is { } typed && typed == decimal.Truncate(typed) && typed <= 365 ? (int)typed : -1;
            SaveOperationsQuoteResponse response = await save.ExecuteAsync(OperationsCurrentUser.Context(http), new SaveOperationsQuoteRequest
            {
                CustomerCode = form.Customer,
                OriginWarehouseId = form.Origin,
                DestinationWarehouseId = form.Destination,
                Delivery = Delivery(form),
                ServiceCode = form.Service,
                Pieces = form.Pieces(),
                Consolidated = form.Consolidated,
                Insured = form.Insured,
                ExtraQuantities = form.Quantities,
                PreferredTotal = preferred,
                Reason = form.Reason,
                ValidDays = days,
                CommandKey = form.Key
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/quotes?done=saved&number=" + Uri.EscapeDataString(response.Number))
                : await QuotesAsync(http, form, response.Error, null, price, list, brand, errors, rate: true);
        });

        staff.MapPost("/quotes/{quoteId:guid}/withdraw", async (HttpContext http, Guid quoteId, [FromForm] string? from, WithdrawOperationsQuoteUseCase withdraw, PriceOperationsShipmentUseCase price,
            ListOperationsQuotesUseCase list, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
        {
            WithdrawOperationsQuoteResponse response = await withdraw.ExecuteAsync(OperationsCurrentUser.Context(http), new WithdrawOperationsQuoteRequest { QuoteId = quoteId }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect(from == "list" ? "/staff/quotes/list?state=withdrawn&done=withdrawn" : "/staff/quotes")
                : await QuotesAsync(http, new OperationsQuoteForm(), response.Error, null, price, list, brand, errors, rate: false);
        });

        return endpoints;
    }

    private static OperationsDeliveryModes Delivery(OperationsQuoteForm form) =>
        form.Delivery == "Pickup" ? OperationsDeliveryModes.Pickup : form.Delivery == "HomeDelivery" ? OperationsDeliveryModes.HomeDelivery : OperationsDeliveryModes.None;

    private static async Task<IResult> QuotesAsync(HttpContext http, OperationsQuoteForm form, Error? error, string? info, PriceOperationsShipmentUseCase price, ListOperationsQuotesUseCase list,
        GetOperationsSignInBrandUseCase brand, OperationsErrorText errors, bool rate)
    {
        PriceOperationsShipmentResponse priced = await price.ExecuteAsync(OperationsCurrentUser.Context(http), new PriceOperationsShipmentRequest
        {
            CustomerCode = form.Customer,
            OriginWarehouseId = form.Origin,
            DestinationWarehouseId = form.Destination,
            Delivery = Delivery(form),
            ServiceCode = form.Service,
            Pieces = rate && error is null ? form.Pieces() : null,
            Consolidated = form.Consolidated,
            Insured = form.Insured,
            ExtraQuantities = form.Quantities
        }, http.RequestAborted);

        // A refused price (no customer, no card) still shows the form: the same use case without pieces gives its choices.
        if (!priced.Succeeded && priced.Error is not null && !priced.Error.Equals(Vixly.Operations.Application.Security.OperationsAccessErrors.SignInRequired))
        {
            error ??= priced.Error;
            priced = await price.ExecuteAsync(OperationsCurrentUser.Context(http), new PriceOperationsShipmentRequest(), http.RequestAborted);
        }

        ListOperationsQuotesResponse quotes = await list.ExecuteAsync(OperationsCurrentUser.Context(http), new ListOperationsQuotesRequest { PageSize = 10 }, http.RequestAborted);
        return OperationsStaffResults.Page<QuotesPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Form = form,
            Ratings = priced.Ratings,
            Quotes = quotes.Quotes,
            Origins = priced.Origins,
            Destinations = priced.Destinations,
            ItemTypes = priced.ItemTypes,
            Services = priced.Services,
            ManualExtras = priced.ManualExtras,
            CustomerName = priced.CustomerName,
            Weight = OperationsChargeWords.Weight(priced.WeightUnit),
            Length = OperationsChargeWords.Length(priced.DimensionUnit),
            Currency = priced.CurrencyCode,
            Denied = !quotes.Succeeded,
            Error = errors.Describe(quotes.Error ?? error),
            Info = info
        });
    }
}
