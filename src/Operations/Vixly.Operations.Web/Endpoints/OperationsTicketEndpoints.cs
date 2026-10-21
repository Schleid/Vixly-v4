using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Vixly.Operations.Application.UseCases.Receiving.AssignOperationsParcel;
using Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;
using Vixly.Operations.Application.UseCases.Tickets.AnswerOperationsTicket;
using Vixly.Operations.Application.UseCases.Tickets.GetOperationsTicket;
using Vixly.Operations.Application.UseCases.Tickets.GetOperationsTicketProof;
using Vixly.Operations.Application.UseCases.Tickets.ListOperationsTickets;
using Vixly.Operations.Domain.Tickets;
using Vixly.Operations.Resources;
using Vixly.Operations.Web.Components.Tickets;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Web.Endpoints;

// =========================================================
// FILE: OperationsTicketEndpoints.cs
//
// PURPOSE:
// The customers' tickets about packages the carrier delivered that are not in their account (Slice 4D step 4, A-6).
//
// RESPONSIBILITIES:
// - /staff/tickets: the queue of the user's warehouses, open first
// - /staff/tickets/{id}: the ticket, the undeclared packages that resemble it, answer, resolve, or give the package found to the customer
// - /staff/tickets/{id}/proof: the carrier's proof of delivery
//
// IMPORTANT NOTES:
// - Forms with antiforgery and the form guard (ADR-0019); the use cases check the permission and the warehouse
// - Giving a package is the assignment of Slice 3B (with how staff checked it), then the ticket is resolved with it
// =========================================================
public static class OperationsTicketEndpoints
{
    public static IEndpointRouteBuilder MapOperationsTicketEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder staff = endpoints.MapGroup("/staff/tickets").RequireAuthorization().RequireFormToken();

        staff.MapGet("/", async (HttpContext http, Guid? warehouse, string? status, int? page, ListOperationsTicketsUseCase list, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            OperationsTicketStatus? shown = status switch
            {
                "answered" => OperationsTicketStatus.Answered,
                "resolved" => OperationsTicketStatus.Resolved,
                "all" => null,
                _ => OperationsTicketStatus.Open
            };
            ListOperationsTicketsResponse response = await list.ExecuteAsync(OperationsCurrentUser.Context(http),
                new ListOperationsTicketsRequest { WarehouseId = warehouse, Status = shown, Page = page ?? 1 }, http.RequestAborted);
            return OperationsStaffResults.Page<TicketsPage>(http, new
            {
                Company = await OperationsStaffResults.CompanyAsync(http, brand),
                Tickets = response.Tickets,
                TotalCount = response.TotalCount,
                Warehouses = response.Warehouses,
                Warehouse = warehouse,
                Status = status is "answered" or "resolved" or "all" ? status : "open",
                PageNumber = Math.Max(page ?? 1, 1),
                Error = errors.Describe(response.Error)
            });
        });

        staff.MapGet("/{ticketId:guid}", async (HttpContext http, Guid ticketId, string? done, GetOperationsTicketUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors, IStringLocalizer<OperationsText> text) =>
            await TicketAsync(http, ticketId, null, done is "answered" or "resolved" or "linked" ? text["ui.tickets.done." + done].Value : null, get, brand, errors));

        staff.MapPost("/{ticketId:guid}/answer", async (HttpContext http, Guid ticketId, [FromForm] string? message, [FromForm] bool? resolve, [FromForm] string? key,
            AnswerOperationsTicketUseCase answer,
            GetOperationsTicketUseCase get, GetOperationsSignInBrandUseCase brand, OperationsErrorText errors) =>
        {
            AnswerOperationsTicketResponse response = await answer.ExecuteAsync(OperationsCurrentUser.Context(http), new AnswerOperationsTicketRequest
            {
                TicketId = ticketId,
                Message = message,
                Resolve = resolve == true,
                PortalSlug = OperationsStaffResults.PortalSlug(http),
                CommandKey = key ?? string.Empty
            }, http.RequestAborted);
            return response.Succeeded
                ? Results.Redirect("/staff/tickets/" + ticketId + "?done=" + (resolve == true ? "resolved" : "answered"))
                : await TicketAsync(http, ticketId, response.Error, null, get, brand, errors);
        });

        // The package found: given to the ticket's customer (how staff checked it is recorded), then the ticket is resolved with it.
        staff.MapPost("/{ticketId:guid}/link", async (HttpContext http, Guid ticketId, [FromForm] Guid? parcel, [FromForm] string? verifiedBy, [FromForm] string? key,
            [FromForm] string? message, AssignOperationsParcelUseCase assign, AnswerOperationsTicketUseCase answer, GetOperationsTicketUseCase get, GetOperationsSignInBrandUseCase brand,
            OperationsErrorText errors) =>
        {
            GetOperationsTicketResponse ticket = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsTicketRequest { TicketId = ticketId }, http.RequestAborted);

            if (ticket.Ticket is null || parcel is not { } parcelId)
            {
                return await TicketAsync(http, ticketId, ticket.Error ?? OperationsTicketErrors.ParcelNotTheirs, null, get, brand, errors);
            }

            AssignOperationsParcelResponse assigned = await assign.ExecuteAsync(OperationsCurrentUser.Context(http), new AssignOperationsParcelRequest
            {
                ParcelId = parcelId,
                CustomerCode = ticket.CustomerCode ?? string.Empty,
                VerifiedBy = verifiedBy ?? string.Empty,
                CommandKey = key ?? string.Empty,
                PortalSlug = OperationsStaffResults.PortalSlug(http)
            }, http.RequestAborted);

            // Refused: shown, nothing saved. Already given (from the parcel page, or a try that stopped half-way): the ticket is resolved with it.
            if (assigned.Error is { } refused && !refused.Equals(Vixly.Operations.Domain.Receiving.OperationsReceivingErrors.NotUnidentified))
            {
                return await TicketAsync(http, ticketId, refused, null, get, brand, errors);
            }

            AnswerOperationsTicketResponse resolved = await answer.ExecuteAsync(OperationsCurrentUser.Context(http), new AnswerOperationsTicketRequest
            {
                TicketId = ticketId,
                Message = message,
                Resolve = true,
                ParcelId = parcelId,
                PortalSlug = OperationsStaffResults.PortalSlug(http),
                CommandKey = key ?? string.Empty
            }, http.RequestAborted);

            return resolved.Succeeded
                ? Results.Redirect("/staff/tickets/" + ticketId + "?done=linked")
                : await TicketAsync(http, ticketId, resolved.Error, null, get, brand, errors);
        });

        staff.MapGet("/{ticketId:guid}/proof", async (HttpContext http, Guid ticketId, GetOperationsTicketProofUseCase proof) =>
        {
            GetOperationsTicketProofResponse response = await proof.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsTicketProofRequest { TicketId = ticketId },
                http.RequestAborted);

            if (!response.Succeeded)
            {
                return Results.NotFound();
            }

            // The customer's file: a download, never run as a page of this site (as the documents answering holds).
            http.Response.Headers.CacheControl = "private, no-store";
            http.Response.Headers.XContentTypeOptions = "nosniff";
            http.Response.Headers.ContentSecurityPolicy = "default-src 'none'; sandbox";
            string extension = response.ContentType switch { "application/pdf" => ".pdf", "image/png" => ".png", _ => ".jpg" };
            return Results.File(response.Content, response.ContentType, "proof-of-delivery" + extension);
        });

        return endpoints;
    }

    private static async Task<IResult> TicketAsync(HttpContext http, Guid ticketId, Error? error, string? info, GetOperationsTicketUseCase get, GetOperationsSignInBrandUseCase brand,
        OperationsErrorText errors)
    {
        GetOperationsTicketResponse response = await get.ExecuteAsync(OperationsCurrentUser.Context(http), new GetOperationsTicketRequest { TicketId = ticketId }, http.RequestAborted);

        if (response.Ticket is not { } ticket)
        {
            return Results.NotFound();
        }

        return OperationsStaffResults.Page<TicketPage>(http, new
        {
            Company = await OperationsStaffResults.CompanyAsync(http, brand),
            Ticket = ticket,
            WarehouseCode = response.Warehouse?.Code,
            CustomerCode = response.CustomerCode,
            CustomerName = response.CustomerName,
            Lookalikes = response.Lookalikes,
            CanAnswer = response.CanAnswer,
            Error = errors.Describe(error),
            Info = info
        });
    }
}
