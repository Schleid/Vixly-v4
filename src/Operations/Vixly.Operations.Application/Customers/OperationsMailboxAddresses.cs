using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.Customers;

// =========================================================
// FILE: OperationsMailboxAddresses.cs
//
// PURPOSE:
// Works out a customer's mailbox addresses from the receiving warehouses (Slice 2, D-C2).
//
// RESPONSIBILITIES:
// - One address per active receiving warehouse that accepts deliveries
//
// IMPORTANT NOTES:
// - Computed, never stored: a new warehouse appears for every customer at once
// - No code yet (unconfirmed email): no address
// =========================================================
public static class OperationsMailboxAddresses
{
    public static IReadOnlyList<OperationsMailboxAddress> For(OperationsCustomer customer, IEnumerable<OperationsWarehouse> warehouses)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(warehouses);

        if (customer.Code is null)
        {
            return Array.Empty<OperationsMailboxAddress>();
        }

        return warehouses
            .Where(warehouse => warehouse.Status == OperationsLocationStatus.Active && warehouse.AcceptingDeliveries && warehouse.Roles.HasFlag(OperationsWarehouseRoles.Receiving))
            .OrderBy(warehouse => warehouse.Name, StringComparer.CurrentCulture)
            .Select(warehouse => new OperationsMailboxAddress(
                warehouse.Id,
                warehouse.Name,
                customer.FullName + " " + customer.Code,
                warehouse.Address.Line1,
                warehouse.Address.Line2,
                warehouse.Address.City,
                warehouse.Address.Region,
                warehouse.Address.PostalCode,
                warehouse.Address.CountryCode))
            .ToList();
    }
}
