using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
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

        portal.MapGet("/", async (HttpContext http, string? done, string? number, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
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

            CustomerCall<CustomerCreditAccountDto> credit = await api.GetCreditAccountAsync(tenant, session, http, http.RequestAborted);

            return new RazorComponentResult<InvoicesPage>(new
            {
                Brand = await api.GetBrandAsync(tenant, http.RequestAborted),
                Invoices = call.Value,
                Credit = credit.Value is { HasAccount: true } account ? account : null,
                Error = call.Value is null ? CustomerPortalEndpoints.Describe(text, call) : null,
                Info = done == "reported" ? Vixly.Shared.Localization.VixlyText.Format(text, "ui.pay.reported", "number", Clip(number)) : null
            });
        });

        // Owner, 2026-10-27: pay on the portal with the methods the company offers there; with one way only, it opens directly.
        portal.MapGet("/{invoiceId:guid}/pay", async (HttpContext http, Guid invoiceId, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session)
            {
                return Results.Redirect("/customer/sign-in");
            }

            CustomerCall<CustomerInvoicesDto> call = await api.ListInvoicesAsync(tenant, session, http, http.RequestAborted);

            if (call.Value?.Invoices.FirstOrDefault(item => item.InvoiceId == invoiceId) is not { } invoice)
            {
                return Results.Redirect("/customer/invoices");
            }

            IReadOnlyList<string> methods = CustomerPayMethods.For(call.Value, invoice);

            if (methods.Count == 0)
            {
                return Results.Redirect("/customer/invoices");
            }

            if (methods is [var only] && only != CustomerPayMethods.Card)
            {
                return Results.Redirect("/customer/invoices/" + invoiceId + "/pay/" + only);
            }

            return new RazorComponentResult<PayChoicePage>(new { Brand = await api.GetBrandAsync(tenant, http.RequestAborted), Invoice = invoice, Methods = methods });
        });

        portal.MapGet("/{invoiceId:guid}/pay/{method}", async (HttpContext http, Guid invoiceId, string method, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session)
            {
                return Results.Redirect("/customer/sign-in");
            }

            if (method is not (CustomerPayMethods.Zelle or CustomerPayMethods.BankTransfer))
            {
                return Results.NotFound();
            }

            CustomerCall<CustomerPayDto> call = await api.GetPayAsync(tenant, session, invoiceId, method, http, http.RequestAborted);
            return new RazorComponentResult<PayMethodPage>(new
            {
                Brand = await api.GetBrandAsync(tenant, http.RequestAborted),
                InvoiceId = invoiceId,
                Method = method,
                Pay = call.Value,
                Error = call.Value is null ? CustomerPortalEndpoints.Describe(text, call) : null
            });
        });

        portal.MapPost("/{invoiceId:guid}/pay/{method}", async (HttpContext http, Guid invoiceId, string method, [FromForm] string? reference, [FromForm] string? terms,
            [FromForm] string? key, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session)
            {
                return Results.Redirect("/customer/sign-in");
            }

            if (method is not (CustomerPayMethods.Zelle or CustomerPayMethods.BankTransfer))
            {
                return Results.NotFound();
            }

            Guid requestKey = Guid.TryParseExact(key, "N", out Guid parsed) ? parsed : Guid.NewGuid();
            CustomerCall<CustomerPayReportDto> sent = await api.ReportPayAsync(tenant, session, invoiceId, method,
                new CustomerPayReportBody(reference, terms == "true", requestKey), http, http.RequestAborted);

            if (sent.Value is { } reported)
            {
                return Results.Redirect("/customer/invoices?done=reported&number=" + Uri.EscapeDataString(reported.InvoiceNumber));
            }

            CustomerCall<CustomerPayDto> details = await api.GetPayAsync(tenant, session, invoiceId, method, http, http.RequestAborted);
            return new RazorComponentResult<PayMethodPage>(new
            {
                Brand = await api.GetBrandAsync(tenant, http.RequestAborted),
                InvoiceId = invoiceId,
                Method = method,
                Pay = details.Value,
                Key = requestKey.ToString("N"),
                Reference = reference,
                Error = CustomerPortalEndpoints.Describe(text, sent)
            });
        });

        portal.MapGet("/zelle-qr", async (HttpContext http, CustomerOperationsApi api) =>
        {
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session
                || await api.GetZelleQrAsync(tenant, session, http, http.RequestAborted) is not { } picture)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            return Results.Bytes(picture.Content, picture.ContentType);
        });

        // Slice 5F step 2b (K-8): the credit account's statement.
        portal.MapGet("/statement", async (HttpContext http, CustomerOperationsApi api) =>
        {
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session
                || await api.GetCreditStatementAsync(tenant, session, http, http.RequestAborted) is not { } document)
            {
                return Results.NotFound();
            }

            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            return Results.File(document.Content, "application/pdf", document.FileName);
        });

        // Slice 5F step 4: pay by card. The charge is made here, then its page sends the customer on to Stripe and Stripe sends them back to it.
        portal.MapPost("/{invoiceId:guid}/card", async (HttpContext http, Guid invoiceId, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session)
            {
                return Results.Redirect("/customer/sign-in");
            }

            CustomerCall<CustomerCardStartDto> call = await api.StartCardPaymentAsync(tenant, session, invoiceId,
                "https://" + http.Request.Host.Value + "/customer/invoices/card/", http, http.RequestAborted);

            if (call.Value is { } started)
            {
                return Results.Redirect("/customer/invoices/card/" + started.ChargeId);
            }

            return new RazorComponentResult<CardPaymentPage>(new
            {
                Brand = await api.GetBrandAsync(tenant, http.RequestAborted),
                Payment = (CustomerCardPaymentDto?)null,
                Error = CustomerPortalEndpoints.Describe(text, call)
            });
        });

        portal.MapGet("/card/{chargeId:guid}", async (HttpContext http, Guid chargeId, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (CustomerTenantMiddleware.Host(http)?.TenantId is not { } tenant || CustomerSessionCookie.Read(http.Request) is not { } session)
            {
                return Results.Redirect("/customer/sign-in");
            }

            CustomerCall<CustomerCardPaymentDto> call = await api.GetCardPaymentAsync(tenant, session, chargeId, http, http.RequestAborted);

            if (call.Value is null && call.ErrorCode == "ops.cards.charge_not_found")
            {
                return Results.NotFound();
            }

            return new RazorComponentResult<CardPaymentPage>(new
            {
                Brand = await api.GetBrandAsync(tenant, http.RequestAborted),
                Payment = call.Value,
                Error = call.Value is null ? CustomerPortalEndpoints.Describe(text, call) : null
            });
        });

        // The monthly notice links here: the account is shown with the invoices.
        endpoints.MapGet("/account", () => Results.Redirect("/customer/invoices#account"));

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

    // The invoice number shown back after a payment is reported: letters, digits and dashes only, never more than 30.
    private static string Clip(string? number) =>
        new string((number ?? string.Empty).Where(character => char.IsAsciiLetterOrDigit(character) || character == '-').Take(30).ToArray());
}
