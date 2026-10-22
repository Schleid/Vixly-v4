using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Localization;
using Vixly.Contracts.Operations.Tickets;
using Vixly.Customer.Resources;
using Vixly.Customer.Web.Components.Portal;
using Vixly.Customer.Web.Operations;
using Vixly.Customer.Web.Tenancy;

namespace Vixly.Customer.Web.Portal;

// =========================================================
// FILE: CustomerTicketEndpoints.cs
//
// PURPOSE:
// The customer's tickets: a package the carrier delivered that is not in the account (Slice 4D step 4, A-6).
//
// RESPONSIBILITIES:
// - /customer/tickets: the list; /customer/tickets/new: open one, with the carrier's proof of delivery
// - /customer/tickets/{id}: the ticket, its messages and the answer form
//
// IMPORTANT NOTES:
// - Signed-in pages; Operations checks the session and the ownership on every call (S-3)
// - The customer never searches the warehouse: staff search and link the package
// - Nothing is cached (no-store); forms carry antiforgery (ADR-0019)
// =========================================================
public static class CustomerTicketEndpoints
{
    private const int MaxProofBytes = 10 * 1024 * 1024;

    public static IEndpointRouteBuilder MapCustomerTicketEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder portal = endpoints.MapGroup("/tickets");
        portal.WithMetadata(new RequireAntiforgeryTokenAttribute());
        portal.AddEndpointFilter(async (context, next) =>
        {
            context.HttpContext.Response.Headers.CacheControl = "no-store";
            return context.HttpContext.Features.Get<IAntiforgeryValidationFeature>() is { IsValid: false } ? Results.BadRequest() : await next(context);
        });

        portal.MapGet("/", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) => await ListAsync(http, api, text, false, null, null));

        portal.MapGet("/new", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) => await ListAsync(http, api, text, true, null, null));

        portal.MapPost("/new", async (HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            IFormCollection form;

            // A file above the form limit fails while the form is read: the customer sees the size rule, not an error page.
            try
            {
                form = await http.Request.ReadFormAsync(http.RequestAborted);
            }
            catch (Exception exception) when (exception is InvalidDataException or BadHttpRequestException)
            {
                return await ListAsync(http, api, text, true, null, text["ops.tickets.proof_invalid"].Value);
            }

            byte[]? proof = null;

            if (form.Files.GetFile("proof") is { Length: > 0 } file)
            {
                if (file.Length > MaxProofBytes)
                {
                    return await ListAsync(http, api, text, true, Entered(form), text["ops.tickets.proof_invalid"].Value);
                }

                using MemoryStream buffer = new((int)file.Length);
                await file.CopyToAsync(buffer, http.RequestAborted);
                proof = buffer.ToArray();
            }

            CustomerTicketBody body = Entered(form) with { Proof = proof };
            CustomerCall<CustomerTicketCreatedDto> call = await api.CreateTicketAsync(signedIn.Tenant, signedIn.Session, body, http, http.RequestAborted);

            return call.Value is { } created
                ? Results.Redirect("/customer/tickets/" + created.TicketId + "?done=opened")
                : await ListAsync(http, api, text, true, body with { Proof = null }, CustomerPortalEndpoints.Describe(text, call));
        }).WithFormOptions(multipartBodyLengthLimit: MaxProofBytes + (1024 * 1024));

        portal.MapGet("/{ticketId:guid}", async (HttpContext http, Guid ticketId, string? done, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
            await TicketAsync(http, ticketId, api, null, done is "opened" or "sent" ? text["ui.tickets.done." + done].Value : null));

        portal.MapPost("/{ticketId:guid}/messages", async (HttpContext http, Guid ticketId, CustomerOperationsApi api, IStringLocalizer<CustomerText> text) =>
        {
            if (Signed(http) is not { } signedIn)
            {
                return Results.Redirect("/customer/sign-in");
            }

            IFormCollection form = await http.Request.ReadFormAsync(http.RequestAborted);
            CustomerCall<object> call = await api.ReplyTicketAsync(signedIn.Tenant, signedIn.Session, ticketId, new CustomerTicketReplyBody(form["message"].ToString(), form["key"].ToString()), http, http.RequestAborted);

            return call.Succeeded
                ? Results.Redirect("/customer/tickets/" + ticketId + "?done=sent")
                : await TicketAsync(http, ticketId, api, CustomerPortalEndpoints.Describe(text, call), null);
        });

        return endpoints;
    }

    private static CustomerTicketBody Entered(IFormCollection form) => new(
        Guid.TryParse(form["warehouse"], out Guid warehouse) ? warehouse : null,
        form["carrier"].ToString(),
        form["tracking"].ToString(),
        form["store"].ToString(),
        form["order"].ToString(),
        DateOnly.TryParseExact(form["delivered"].ToString(), "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out DateOnly day) ? day : null,
        form["contents"].ToString(),
        form["message"].ToString(),
        null);

    private static async Task<IResult> ListAsync(HttpContext http, CustomerOperationsApi api, IStringLocalizer<CustomerText> text, bool form, CustomerTicketBody? entered, string? error)
    {
        if (Signed(http) is not { } signedIn)
        {
            return Results.Redirect("/customer/sign-in");
        }

        CustomerCall<CustomerTicketsDto> call = await api.ListTicketsAsync(signedIn.Tenant, signedIn.Session, http, http.RequestAborted);

        if (call.ErrorCode == "ops.customers.session_required")
        {
            CustomerSessionCookie.Delete(http.Response);
            return Results.Redirect("/customer/sign-in");
        }

        object parameters = new
        {
            Brand = await api.GetBrandAsync(signedIn.Tenant, http.RequestAborted),
            Tickets = call.Value,
            Entered = entered,
            Error = error ?? (call.Value is null ? CustomerPortalEndpoints.Describe(text, call) : null)
        };
        return form ? new RazorComponentResult<TicketFormPage>(parameters) : new RazorComponentResult<TicketsPage>(parameters);
    }

    private static async Task<IResult> TicketAsync(HttpContext http, Guid ticketId, CustomerOperationsApi api, string? error, string? info)
    {
        if (Signed(http) is not { } signedIn)
        {
            return Results.Redirect("/customer/sign-in");
        }

        CustomerCall<CustomerTicketDto> call = await api.GetTicketAsync(signedIn.Tenant, signedIn.Session, ticketId, http, http.RequestAborted);

        if (call.ErrorCode == "ops.customers.session_required")
        {
            CustomerSessionCookie.Delete(http.Response);
            return Results.Redirect("/customer/sign-in");
        }

        return call.Value is { } ticket
            ? new RazorComponentResult<TicketPage>(new { Brand = await api.GetBrandAsync(signedIn.Tenant, http.RequestAborted), Ticket = ticket, Error = error, Info = info })
            : Results.NotFound();
    }

    private static (Guid Tenant, string Session)? Signed(HttpContext http) =>
        CustomerTenantMiddleware.Host(http)?.TenantId is { } tenant && CustomerSessionCookie.Read(http.Request) is { } session ? (tenant, session) : null;
}
