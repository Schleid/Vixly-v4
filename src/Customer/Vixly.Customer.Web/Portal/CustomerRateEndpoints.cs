using System.Globalization;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Contracts.Operations.Rates;
using Vixly.Customer.Resources;
using Vixly.Customer.Web.Components.Portal;
using Vixly.Customer.Web.Operations;
using Vixly.Customer.Web.Tenancy;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerRateEndpoints.cs
//
// PURPOSE:
// The customer's rate calculator and shipment estimate (Slice 4 design section 8).
//
// RESPONSIBILITIES:
// - /customer/rates: the calculator and the customer's quotes; ?shipment= the estimate of that shipment
// - POST /customer/rates: an estimate for the typed package
//
// IMPORTANT NOTES:
// - Signed-in pages; Operations checks the session and the shipment's owner (S-3)
// - Nothing is cached (no-store); forms carry antiforgery (ADR-0019)
// =========================================================
public static class CustomerRateEndpoints
{
    public static IEndpointRouteBuilder MapCustomerRateEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder rates = endpoints.MapGroup("/rates");
        rates.WithMetadata(new RequireAntiforgeryTokenAttribute());
        rates.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return context.HttpContext.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: false } ? Results.BadRequest() : await next(context);
        });

        rates.MapGet("/", async (HttpContext http, Guid? shipment, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
            await RatesAsync(http, api, text, new CustomerRatesBody(null, null, false, false, shipment), null));

        rates.MapPost("/", async (HttpContext http, [FromForm] string? origin, [FromForm] string? destination, [FromForm] string? weight, [FromForm] string? length, [FromForm] string? width, [FromForm] string? height,
            [FromForm] string? value, [FromForm] string? consolidated, [FromForm] string? insured, [FromForm] string? home, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            CustomerPieceBody piece = new(Number(weight), Optional(length), Optional(width), Optional(height), Optional(value));
            Guid? from = Guid.TryParse(origin, out Guid originId) ? originId : null;
            Guid? to = Guid.TryParse(destination, out Guid destinationId) ? destinationId : null;
            return await RatesAsync(http, api, text, new CustomerRatesBody(null, new[] { piece }, consolidated == "true", insured == "true", null, to, home == "true", from),
                new Typed(from, to, weight, length, width, height, value, consolidated == "true", insured == "true", home == "true"));
        });

        // The Pricing page (owner, 2026-10-21): the company's prices, the customer's own first.
        endpoints.MapGet("/pricing", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            http.Response.Headers.CacheControl = "no-store";

            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session)
            {
                return Results.Redirect("/customer/sign-in");
            }

            CustomerCall<Vixly.Contracts.Operations.Rates.CustomerPricingDto> call = await api.GetPricingAsync(tenant, session, http, http.RequestAborted);

            if (call.ErrorCode == "ops.customers.session_required")
            {
                CustomerSessionCookie.Delete(http.Response);
                return Results.Redirect("/customer/sign-in");
            }

            return new RazorComponentResult<PricingPage>(new
            {
                Brand = await api.GetBrandAsync(tenant, http.RequestAborted),
                Pricing = call.Value,
                Error = call.Value is null ? CustomerPortalEndpoints.Describe(text, call) : null
            });
        });

        return endpoints;
    }

    private static async Task<IResult> RatesAsync(HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text, CustomerRatesBody body, Typed? typed)
    {
        if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session)
        {
            return Results.Redirect("/customer/sign-in");
        }

        CustomerCall<CustomerRatesDto> call = await api.GetRatesAsync(tenant, session, body, http, http.RequestAborted);

        if (call.ErrorCode == "ops.customers.session_required")
        {
            CustomerSessionCookie.Delete(http.Response);
            return Results.Redirect("/customer/sign-in");
        }

        if (call.ErrorCode == "ops.shipments.not_found")
        {
            return Results.NotFound();
        }

        string? error = call.Succeeded ? null : CustomerPortalEndpoints.Describe(text, call);

        // A refused calculation still shows the form: ask again without the package.
        if (call.Value is null && !call.Unavailable)
        {
            call = await api.GetRatesAsync(tenant, session, new CustomerRatesBody(null, null, false, false, null), http, http.RequestAborted);
        }

        return call.Value is { } rates
            ? new RazorComponentResult<RatesPage>(new
            {
                Brand = await api.GetBrandAsync(tenant, http.RequestAborted),
                Rates = rates,
                ShipmentId = body.ShipmentId,
                Origin = typed?.Origin,
                Destination = typed?.Destination,
                Weight = typed?.Weight,
                Length = typed?.Length,
                Width = typed?.Width,
                Height = typed?.Height,
                Value = typed?.Value,
                Consolidated = typed?.Consolidated ?? false,
                Insured = typed?.Insured ?? false,
                HomeDelivery = typed?.HomeDelivery ?? false,
                Error = error
            })
            : Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable);
    }

    // A plain number (a dot for decimals), or nothing; anything else is sent as -1 so Operations refuses it.
    private static decimal? Number(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null
        : value.Trim().Length <= 20 && value.Trim().All(character => char.IsAsciiDigit(character) || character == '.')
            && decimal.TryParse(value.Trim(), NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal number) ? number : -1m;

    private static decimal? Optional(string? value) => Number(value) ?? 0m;

    private sealed record Typed(Guid? Origin, Guid? Destination, string? Weight, string? Length, string? Width, string? Height, string? Value, bool Consolidated, bool Insured, bool HomeDelivery);
}
