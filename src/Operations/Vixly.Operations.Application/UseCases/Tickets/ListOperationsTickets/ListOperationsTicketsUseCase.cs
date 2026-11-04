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

namespace Vixly.Operations.Application.UseCases.Tickets.ListOperationsTickets;

// =========================================================
// FILE: ListOperationsTicketsUseCase.cs
//
// PURPOSE:
// The ticket desk: the tickets of the departments the staff member belongs to, escalated and oldest first (Slice 4D step 4; owner, 2026-10-29, ticket desk).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Each topic shows at the warehouses where the person's roles cover its department (OperationsTicketScope)
// - Nobody in any department: "your role does not allow this"
// =========================================================
public sealed class ListOperationsTicketsUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly Vixly.Operations.Application.Tickets.OperationsTicketScope _scope;

    private readonly IOperationsTicketRepository _tickets;

    private readonly IOperationsUserRepository _users;

    private readonly IOperationsSettingsRepository _settings;

    public ListOperationsTicketsUseCase(
        OperationsOwnerCheck owner,
        Vixly.Operations.Application.Tickets.OperationsTicketScope scope,
        IOperationsTicketRepository tickets,
        IOperationsUserRepository users,
        IOperationsSettingsRepository settings)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<ListOperationsTicketsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsTicketsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsTicketsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        (IReadOnlyDictionary<OperationsTicketTopic, IReadOnlyCollection<Guid>> scope, IReadOnlyList<OperationsWarehouse> warehouses) = await _scope.ForAsync(access, cancellationToken);

        if (scope.Count == 0)
        {
            return ListOperationsTicketsResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        Vixly.Operations.Application.Tickets.OperationsTicketDeskFilter filter = new(request.State, request.Topic, request.WarehouseId, request.Mine ? access.User.Id : null);
        (IReadOnlyList<OperationsTicket> items, int total) = await _tickets.ListAsync(scope, filter, Math.Max(request.Page, 1), PageSize, cancellationToken);
        Dictionary<Guid, string> people = (await _users.ListAsync(null, 1, 1000, cancellationToken)).Items.ToDictionary(user => user.Id, user => user.FullName);
        Vixly.Operations.Domain.Settings.OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        return ListOperationsTicketsResponse.Success(items, total, warehouses, scope.Keys.OrderBy(topic => topic).ToList(), people,
            (settings?.TicketTakeHours ?? OperationsTicket.TakeHoursDefault, settings?.TicketResolveDays ?? OperationsTicket.ResolveDaysDefault));
    }

    public const int PageSize = 25;
}
