using Microsoft.AspNetCore.Antiforgery;
using Vixly.Customer.Web.Operations;
using Vixly.Customer.Web.Tenancy;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerTourEndpoints.cs
// PURPOSE:
// Replays and dismisses the customer's optional welcome tour.
// RESPONSIBILITIES:
// - Forward only the verified tenant and session to Operations.
// IMPORTANT NOTES:
// - GET never dismisses a tour; POST requires antiforgery.
// - A failed save keeps the tour available with an explanation.
// =========================================================
public static class CustomerTourEndpoints
{
    public static IEndpointRouteBuilder MapCustomerTourEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/tour", async (HttpContext http, CustomerOperationsApi api) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session)
                return Results.Redirect("/customer/sign-in");
            var me = await api.GetMeAsync(tenant, session, http, http.RequestAborted);
            if (me.Value is null) return Results.Redirect("/customer/sign-in");
            return Results.Redirect(me.Value.HomeAddress is null ? "/customer/address" : "/customer/?tour=1");
        });
        endpoints.MapPost("/tour/dismiss", async (HttpContext http, CustomerOperationsApi api) =>
        {
            http.Response.Headers.CacheControl = "no-store";
            if (http.Features.Get<IAntiforgeryValidationFeature>() is not { IsValid: true }) return Results.BadRequest();
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session)
                return Results.Redirect("/customer/sign-in");
            var result = await api.DismissWelcomeTourAsync(tenant, session, http, http.RequestAborted);
            if (result.ErrorCode == "ops.customers.session_required")
            {
                CustomerSessionCookie.Delete(http.Response);
                return Results.Redirect("/customer/sign-in");
            }
            return Results.Redirect(result.Succeeded ? "/customer/" : "/customer/?tour=1&tourError=true");
        }).WithMetadata(new RequireAntiforgeryTokenAttribute());
        return endpoints;
    }
}
