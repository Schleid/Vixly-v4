using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;

namespace Vixly.Operations.Application.Tickets;

// =========================================================
// FILE: OperationsTicketDepartments.cs
//
// PURPOSE:
// Which department handles a ticket's topic, through the roles companies already have (owner, 2026-10-29, ticket desk T-1).
//
// RESPONSIBILITIES:
// - Name the permissions that see a topic's tickets, and those that answer them
// - Say whether a person sees or answers a ticket at its warehouse
//
// IMPORTANT NOTES:
// - Package: the warehouse (view parcels to see, assign parcels to answer), as before the desk
// - Billing: Finance (invoices or payments); pickup and delivery: customer service or the branch's shipment staff;
//   claims: customer service or insurance; other: customer service
// - The Owner and the Administrators see and answer everything (they hold every permission)
// =========================================================
public static class OperationsTicketDepartments
{
    public static IReadOnlyList<OperationsTicketTopic> Topics { get; } = Enum.GetValues<OperationsTicketTopic>();

    public static IReadOnlyList<string> See(OperationsTicketTopic topic) => topic switch
    {
        OperationsTicketTopic.Package => new[] { OperationsPermissions.ParcelsView },
        OperationsTicketTopic.Billing => new[] { OperationsPermissions.InvoicesView, OperationsPermissions.PaymentsTake },
        OperationsTicketTopic.PickupDelivery => new[] { OperationsPermissions.CustomersManage, OperationsPermissions.ShipmentsManage },
        OperationsTicketTopic.Claim => new[] { OperationsPermissions.CustomersManage, OperationsPermissions.InsuranceManage },
        _ => new[] { OperationsPermissions.CustomersManage }
    };

    public static IReadOnlyList<string> Answer(OperationsTicketTopic topic) =>
        topic == OperationsTicketTopic.Package ? new[] { OperationsPermissions.ParcelsAssign } : See(topic);

    public static bool Sees(OperationsAccessView access, OperationsTicketTopic topic, OperationsWarehouse warehouse) => Covers(access, See(topic), warehouse);

    public static bool Answers(OperationsAccessView access, OperationsTicketTopic topic, OperationsWarehouse warehouse) => Covers(access, Answer(topic), warehouse);

    private static bool Covers(OperationsAccessView access, IReadOnlyList<string> permissions, OperationsWarehouse warehouse)
    {
        ArgumentNullException.ThrowIfNull(access);
        ArgumentNullException.ThrowIfNull(warehouse);
        return permissions.Any(permission => access.CoversWarehouse(permission, warehouse.Id, warehouse.BranchId));
    }
}
