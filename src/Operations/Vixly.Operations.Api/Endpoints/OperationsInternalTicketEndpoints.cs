using Vixly.Contracts.Operations.Internal;
using Vixly.Contracts.Operations.Packages;
using Vixly.Contracts.Operations.Tickets;
using Vixly.Operations.Api.Internal;
using Vixly.Operations.Application.UseCases.Tickets.CreateMyOperationsTicket;
using Vixly.Operations.Application.UseCases.Tickets.GetMyOperationsTicket;
using Vixly.Operations.Application.UseCases.Tickets.ListMyOperationsTickets;
using Vixly.Operations.Application.UseCases.Tickets.ReplyMyOperationsTicket;
using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Api.Endpoints;

// =========================================================
// FILE: OperationsInternalTicketEndpoints.cs
//
// PURPOSE:
// The customer portal's ticket calls: a package the carrier delivered that is not in the account (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - List, open, read and answer the customer's own tickets
//
// IMPORTANT NOTES:
// - Customer scope only; every read checks ownership (S-3)
// - Staff are never named to the customer; the proof is not sent back (the customer has it)
// =========================================================
public static class OperationsInternalTicketEndpoints
{
    public static IEndpointRouteBuilder MapOperationsInternalTicketEndpoints(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        RouteGroupBuilder tickets = endpoints.MapGroup("/internal/v1/customers/me/tickets")
            .WithMetadata(new OperationsInternalEndpointMetadata(OperationsCallerClaims.CustomerScope, true));

        tickets.MapGet("", async (HttpContext http, ListMyOperationsTicketsUseCase useCase, CancellationToken cancellationToken) =>
        {
            ListMyOperationsTicketsResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http),
                new ListMyOperationsTicketsRequest { SessionToken = OperationsInternalPackageEndpoints.Session(http) }, cancellationToken);

            return response.Succeeded
                ? Results.Ok(new CustomerTicketsDto(response.Tickets.Select(ticket => Ticket(ticket, false)).ToList(),
                    response.Origins.Select(origin => new CustomerChoiceDto(origin.Id, origin.Name, origin.Detail)).ToList()))
                : OperationsApiErrors.Problem(http, response.Error!);
        });

        tickets.MapPost("", async (HttpContext http, CustomerTicketBody body, CreateMyOperationsTicketUseCase useCase, CancellationToken cancellationToken) =>
        {
            CreateMyOperationsTicketResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http), new CreateMyOperationsTicketRequest
            {
                SessionToken = OperationsInternalPackageEndpoints.Session(http),
                WarehouseId = body.WarehouseId,
                Details = new OperationsTicketDetails(body.Carrier, body.TrackingNumber, body.Store, body.OrderNumber, body.DeliveredOn, body.Contents),
                Message = body.Message,
                Proof = body.Proof
            }, cancellationToken);

            return response.Succeeded ? Results.Ok(new CustomerTicketCreatedDto(response.TicketId)) : OperationsApiErrors.Problem(http, response.Error!);
        });

        tickets.MapGet("/{ticketId:guid}", async (HttpContext http, Guid ticketId, GetMyOperationsTicketUseCase useCase, CancellationToken cancellationToken) =>
        {
            GetMyOperationsTicketResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http),
                new GetMyOperationsTicketRequest { SessionToken = OperationsInternalPackageEndpoints.Session(http), TicketId = ticketId }, cancellationToken);
            return response.Ticket is { } ticket ? Results.Ok(Ticket(ticket, true)) : OperationsApiErrors.Problem(http, response.Error!);
        });

        tickets.MapPost("/{ticketId:guid}/messages", async (HttpContext http, Guid ticketId, CustomerTicketReplyBody body, ReplyMyOperationsTicketUseCase useCase,
            CancellationToken cancellationToken) =>
        {
            ReplyMyOperationsTicketResponse response = await useCase.ExecuteAsync(OperationsInternalPackageEndpoints.Context(http),
                new ReplyMyOperationsTicketRequest { SessionToken = OperationsInternalPackageEndpoints.Session(http), TicketId = ticketId, Message = body.Message, CommandKey = body.Key ?? string.Empty }, cancellationToken);
            return response.Succeeded ? Results.NoContent() : OperationsApiErrors.Problem(http, response.Error!);
        });

        return endpoints;
    }

    private static CustomerTicketDto Ticket(OperationsTicket ticket, bool withMessages) =>
        new(ticket.Id, ticket.Status.ToString(), ticket.Carrier, ticket.TrackingNumber, ticket.Store, ticket.OrderNumber, ticket.DeliveredOn, ticket.Contents,
            ticket.ProofKey is not null, ticket.ParcelId, ticket.CreatedUtc, ticket.ResolvedUtc,
            withMessages ? ticket.Messages.Select(message => new CustomerTicketMessageDto(message.FromStaff, message.Text, message.SentUtc)).ToList() : Array.Empty<CustomerTicketMessageDto>());
}
