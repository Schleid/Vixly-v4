using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Tickets.GetOperationsTicket;

// =========================================================
// FILE: GetOperationsTicketUseCase.cs
//
// PURPOSE:
// One ticket for staff: what the customer knows, the messages, and the undeclared packages that resemble it (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Seen by the ticket's department at its warehouse (OperationsTicketDepartments); a package ticket is answered with ops.parcels.assign
// - The department's people are listed so the ticket can be given to a colleague
// =========================================================
public sealed class GetOperationsTicketUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly Vixly.Operations.Application.Tickets.OperationsTicketScope _scope;

    private readonly Vixly.Operations.Application.Tickets.OperationsTicketPeople _people;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsTicketRepository _tickets;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsParcelReader _reader;

    public GetOperationsTicketUseCase(
        OperationsOwnerCheck owner,
        Vixly.Operations.Application.Tickets.OperationsTicketScope scope,
        Vixly.Operations.Application.Tickets.OperationsTicketPeople people,
        IOperationsUserRepository users,
        IOperationsTicketRepository tickets,
        IOperationsCustomerRepository customers,
        OperationsParcelReader reader)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _people = people ?? throw new ArgumentNullException(nameof(people));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<GetOperationsTicketResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsTicketRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsTicketResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tickets.GetAsync(request.TicketId, cancellationToken) is not { } ticket
            || await _scope.SeenAsync(access, ticket, cancellationToken) is not { } warehouse)
        {
            return GetOperationsTicketResponse.Failure(OperationsTicketErrors.NotFound);
        }

        // The customer's code always (staff assign with it); the name only for staff who may see customers.
        Vixly.Operations.Domain.Customers.OperationsCustomer? customer = await _customers.GetAsync(ticket.CustomerId, cancellationToken);
        bool named = (access.Has(OperationsPermissions.CustomersView) || access.Has(OperationsPermissions.CustomersLookup)) || access.IsAdministrator;
        // Lookalikes only for a customer's missing package (ticket desk: other topics have none).
        IReadOnlyList<OperationsParcelSummary> lookalikes = ticket.Status == OperationsTicketStatus.Resolved || ticket.Topic != OperationsTicketTopic.Package || !ticket.FromCustomer
            ? Array.Empty<OperationsParcelSummary>()
            : await _reader.SummariesAsync(await _tickets.ListLookalikesAsync(ticket, 20, cancellationToken), cancellationToken);
        bool canAnswer = Vixly.Operations.Application.Tickets.OperationsTicketDepartments.Answers(access, ticket.Topic, warehouse);

        string? agentName = ticket.AgentUserId is { } agent ? (await _users.GetAsync(agent, cancellationToken))?.FullName : null;
        string? takenByName = ticket.TakenByUserId is { } taker ? (await _users.GetAsync(taker, cancellationToken))?.FullName : null;
        IReadOnlyList<(Guid Id, string Name)> handlers = canAnswer
            ? (await _people.HandlersAsync(ticket.Topic, warehouse, cancellationToken)).Select(user => (user.Id, user.FullName)).ToList()
            : Array.Empty<(Guid, string)>();

        return GetOperationsTicketResponse.Success(ticket, warehouse, customer?.Code, named ? customer?.FullName : null, lookalikes, canAnswer, agentName, takenByName, handlers);
    }
}
