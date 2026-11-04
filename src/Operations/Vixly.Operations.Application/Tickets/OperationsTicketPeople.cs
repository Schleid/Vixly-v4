using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Identity;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.Tickets;

// =========================================================
// FILE: OperationsTicketPeople.cs
//
// PURPOSE:
// The people of a ticket's department: who can take it, and the managers told when it waits too long (owner, 2026-10-29, ticket desk T-1, T-4).
//
// RESPONSIBILITIES:
// - The active staff who answer a topic at a warehouse (to give a ticket to a colleague)
// - The managers: who answer the topic for the whole company; if nobody, the Administrators; if none, the Owner
// - The second escalation also goes to the Owner
//
// IMPORTANT NOTES:
// - Partner agents and outside accounts are never in a department
// - Reads every active grant once per call; companies have tens of staff, not thousands
// =========================================================
public sealed class OperationsTicketPeople
{
    private readonly IOperationsUserRepository _users;

    private readonly IOperationsRoleRepository _roles;

    public OperationsTicketPeople(IOperationsUserRepository users, IOperationsRoleRepository roles)
    {
        _users = users ?? throw new ArgumentNullException(nameof(users));
        _roles = roles ?? throw new ArgumentNullException(nameof(roles));
    }

    public async Task<IReadOnlyList<OperationsUser>> HandlersAsync(OperationsTicketTopic topic, OperationsWarehouse warehouse, CancellationToken cancellationToken) =>
        (await ViewsAsync(cancellationToken)).Where(view => OperationsTicketDepartments.Answers(view, topic, warehouse)).Select(view => view.User).ToList();

    public async Task<IReadOnlyList<OperationsUser>> ManagersAsync(OperationsTicketTopic topic, bool withOwner, CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationsAccessView> views = await ViewsAsync(cancellationToken);
        List<OperationsUser> managers = views
            .Where(view => !view.IsOwner && !view.IsAdministrator && OperationsTicketDepartments.Answer(topic).Any(view.HasTenantWide))
            .Select(view => view.User).ToList();

        if (managers.Count == 0)
        {
            managers = views.Where(view => view.IsAdministrator && !view.IsOwner).Select(view => view.User).ToList();
        }

        if (withOwner || managers.Count == 0)
        {
            managers.AddRange(views.Where(view => view.IsOwner).Select(view => view.User));
        }

        return managers.DistinctBy(user => user.Id).ToList();
    }

    // Shipment cap (owner, 2026-10-29): the plan warnings go to the Owner.
    public async Task<IReadOnlyList<OperationsUser>> OwnersAsync(CancellationToken cancellationToken) =>
        (await ViewsAsync(cancellationToken)).Where(view => view.IsOwner).Select(view => view.User).ToList();

    private async Task<IReadOnlyList<OperationsAccessView>> ViewsAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<OperationsUser> users = (await _users.ListAsync(null, 1, 1000, cancellationToken)).Items
            .Where(user => user.Account.IsActive && !user.IsPartnerAgent && !user.IsOutside).ToList();
        ILookup<Guid, OperationsRoleGrant> grants = (await _roles.ListActiveGrantsAsync(cancellationToken)).ToLookup(grant => grant.UserId);
        Dictionary<Guid, OperationsRole> roles = (await _roles.ListAsync(cancellationToken)).Where(role => role.IsActive).ToDictionary(role => role.Id);

        return users.Select(user => new OperationsAccessView(user, grants[user.Id].Where(grant => roles.ContainsKey(grant.RoleId))
            .Select(grant => (grant, roles[grant.RoleId])).ToList())).ToList();
    }
}
