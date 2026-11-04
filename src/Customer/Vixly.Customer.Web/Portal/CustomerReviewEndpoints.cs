using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Localization;
using Vixly.Contracts.Operations.Customers;
using Vixly.Contracts.Operations.Reviews;
using Vixly.Customer.Resources;
using Vixly.Customer.Web.Components.Portal;
using Vixly.Customer.Web.Operations;
using Vixly.Customer.Web.Tenancy;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerReviewEndpoints.cs
//
// PURPOSE:
// The customer's review of the company (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - /customer/review: read, write or change the review; /customer/review/delete: delete it
// - /customer/review/dismiss: close the home page's invitation
//
// IMPORTANT NOTES:
// - Signed-in pages; Operations checks the session, the company's setting and the finished shipment on every call (S-3)
// - Nothing is cached (no-store); forms carry antiforgery (ADR-0019)
// =========================================================
public static class CustomerReviewEndpoints
{
    public static IEndpointRouteBuilder MapCustomerReviewEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder portal = endpoints.MapGroup("/review");
        portal.WithMetadata(new RequireAntiforgeryTokenAttribute());
        portal.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return context.HttpContext.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: false } ? Results.BadRequest() : await next(context);
        });

        portal.MapGet("/", async (HttpContext http, string? done, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
            await PageAsync(http, api, null, null, done is "saved" or "deleted" ? text["ui.review.done." + done].Value : null));

        portal.MapPost("/", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            CustomerReviewBody body = new(int.TryParse(form["rating"], out int rating) ? rating : null, form["quote"].ToString(), form["place"].ToString(),
                form["name"].ToString(), form["agreed"] == "true");
            CustomerCall<object> call = await api.SaveReviewAsync(signedIn.Tenant, signedIn.Session, body, http, http.RequestAborted);

            return call.Succeeded
                ? Results.Redirect("/customer/review?done=saved")
                : await PageAsync(http, api, body, CustomerPortalEndpoints.Describe(text, call), null);
        });

        portal.MapPost("/delete", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            CustomerCall<object> call = await api.DeleteReviewAsync(signedIn.Tenant, signedIn.Session, http, http.RequestAborted);
            return call.Succeeded
                ? Results.Redirect("/customer/review?done=deleted")
                : await PageAsync(http, api, null, CustomerPortalEndpoints.Describe(text, call), null);
        });

        // From the home page's invitation: it does not come back.
        portal.MapPost("/dismiss", async (HttpContext http, CustomerOperationsApi api) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            await api.DismissReviewInviteAsync(signedIn.Tenant, signedIn.Session, http, http.RequestAborted);
            return Results.Redirect("/customer/");
        });

        return endpoints;
    }

    private static async Task<IResult> PageAsync(HttpContext http, CustomerOperationsApi api, CustomerReviewBody? entered, string? error, string? info)
    {
        if (Signed(http) is not { } signedIn)
        {
            return Results.Redirect("/customer/sign-in");
        }

        // Harmonization H-6: side by side; Operations checks the session on each.
        Task<CustomerCall<CustomerReviewDto>> reviewCall = api.GetReviewAsync(signedIn.Tenant, signedIn.Session, http, http.RequestAborted);
        Task<CustomerCall<CustomerProfileDto>> meCall = api.GetMeAsync(signedIn.Tenant, signedIn.Session, http, http.RequestAborted);
        await Task.WhenAll(reviewCall, meCall);
        CustomerCall<CustomerReviewDto> call = reviewCall.Result;
        CustomerCall<CustomerProfileDto> me = meCall.Result;

        if (call.ErrorCode == "ops.customers.session_required" || me.Value is null)
        {
            CustomerSessionCookie.Delete(http.Response);
            return Results.Redirect("/customer/sign-in");
        }

        return new RazorComponentResult<ReviewPage>(new
        {
            Brand = await api.GetBrandAsync(signedIn.Tenant, http.RequestAborted),
            Review = call.Value,
            Customer = me.Value,
            Entered = entered,
            Error = error ?? (call.Value is null ? CustomerPortalEndpoints.Describe(http.RequestServices.GetRequiredService<IStringLocalizer<CustomerText>>(), call) : null),
            Info = info
        });
    }

    private static (Guid Tenant, string Session)? Signed(HttpContext http) =>
        CustomerTenantMiddleware.Host(http)?.TenantId is { } tenant && CustomerSessionCookie.Read(http.Request) is { } session ? (tenant, session) : null;
}
