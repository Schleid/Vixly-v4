using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Localization;
using Vixly.Contracts.Operations.Billing;
using Vixly.Customer.Resources;
using Vixly.Customer.Web.Components.Portal;
using Vixly.Customer.Web.Operations;
using Vixly.Customer.Web.Tenancy;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerInvoiceEndpoints.cs
//
// PURPOSE:
// The customer's invoices (Slice 5F, I-8): the list and each PDF.
//
// RESPONSIBILITIES:
// - /customer/invoices: the list; /customer/invoices/{id}/pdf: an invoice or one of its credit notes
//
// IMPORTANT NOTES:
// - Signed-in pages; Operations checks the session and the ownership on every call (S-3)
// - Nothing is cached (no-store)
// =========================================================
public static class CustomerInvoiceEndpoints
{
    public static IEndpointRouteBuilder MapCustomerInvoiceEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder portal = endpoints.MapGroup("/invoices");
        portal.WithMetadata(new RequireAntiforgeryTokenAttribute());
        portal.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return context.HttpContext.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: false } ? Results.BadRequest() : await next(context);
        });

        portal.MapGet("/", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session)
            {
                return Results.Redirect("/customer/sign-in");
            }

            CustomerCall<CustomerInvoicesDto> call = await api.ListInvoicesAsync(tenant, session, http, http.RequestAborted);

            if (call.ErrorCode == "ops.customers.session_required")
            {
                CustomerSessionCookie.Delete(http.Response);
                return Results.Redirect("/customer/sign-in");
            }

            return new RazorComponentResult<InvoicesPage>(new
            {
                Brand = await api.GetBrandAsync(tenant, http.RequestAborted),
                Invoices = call.Value,
                Error = call.Value is null ? CustomerPortalEndpoints.Describe(text, call) : null
            });
        });

        portal.MapGet("/{invoiceId:guid}/pdf", async (HttpContext http, Guid invoiceId, Guid? credit, CustomerOperationsApi api) =>
        {
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session
                || await api.GetInvoiceDocumentAsync(tenant, session, invoiceId, credit, http, http.RequestAborted) is not { } document)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            return Results.File(document.Content, "application/pdf", document.FileName);
        });

        return endpoints;
    }
}
