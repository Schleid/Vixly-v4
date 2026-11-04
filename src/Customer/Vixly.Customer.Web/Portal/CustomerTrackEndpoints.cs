using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Localization;
using Vixly.Contracts.Operations.Packages;
using Vixly.Customer.Resources;
using Vixly.Customer.Web.Components.Portal;
using Vixly.Customer.Web.Operations;
using Vixly.Customer.Web.Tenancy;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerTrackEndpoints.cs
//
// PURPOSE:
// The private link of an "out for delivery" notice (Slice 12C, owner H-5).
//
// RESPONSIBILITIES:
// - /customer/track/{token}: the driver bringing the shipment today, without signing in
// - /customer/track/{token}/now: the same for the page's refresh
//
// IMPORTANT NOTES:
// - Anonymous, rate-limited per address (Link); an unknown or old link shows a short message
// - Never cached; the token is only in the address, never stored here
// =========================================================
public static class CustomerTrackEndpoints
{
    public static IEndpointRouteBuilder MapCustomerTrackEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        endpoints.MapGet("/track/{token}", async (HttpContext http, string token, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "no-store";
            Vixly.Contracts.Operations.Customers.CustomerPortalBrandDto? brand = await api.GetBrandAsync(tenant, http.RequestAborted);
            CustomerDriverDto? driver = (await api.GetTrackedAsync(tenant, token, http, http.RequestAborted)).Value;

            return driver is null
                ? (IResult)new RazorComponentResult<MessagePage>(new
                {
                    Brand = brand,
                    Title = text["ui.driver.link_old_title"].Value,
                    Body = text["ui.driver.link_old"].Value,
                    LinkHref = "/customer/sign-in",
                    LinkText = text["ui.driver.sign_in"].Value
                })
                : new RazorComponentResult<TrackPage>(new { Brand = brand, Driver = driver, Refresh = "/customer/track/" + Uri.EscapeDataString(token) });
        }).RequireRateLimiting(CustomerRateLimits.Link);

        endpoints.MapGet("/track/{token}/now", async (HttpContext http, string token, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "no-store";
            CustomerDriverDto? driver = (await api.GetTrackedAsync(tenant, token, http, http.RequestAborted)).Value;
            return driver is null ? Results.NoContent() : Results.Json(CustomerDriverWords.Json(driver, text));
        }).RequireRateLimiting(CustomerRateLimits.Link);

        return endpoints;
    }
}
