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
// - One address: the company's mailbox warehouse (owner, 2026-10-21), never one per warehouse
//
// IMPORTANT NOTES:
// - Computed, never stored: a new mailbox shows for every customer at once
// - Counter drop-offs still happen at any receiving warehouse (owner, 2026-10-21)
// - No code yet (unconfirmed email): no address
// =========================================================
public static class OperationsMailboxAddresses
{
    // The customers' mailbox: the warehouse named as such, else the oldest one that could be (never none while one could be).
    public static OperationsWarehouse? Mailbox(IEnumerable<OperationsWarehouse> warehouses)
    {
        List<OperationsWarehouse> eligible = (warehouses ?? throw new ArgumentNullException(nameof(warehouses))).Where(warehouse => warehouse.CanBeMailbox).ToList();
        return eligible.FirstOrDefault(warehouse => warehouse.IsMailbox) ?? eligible.OrderBy(warehouse => warehouse.CreatedUtc).ThenBy(warehouse => warehouse.Code, StringComparer.Ordinal).FirstOrDefault();
    }

    public static IReadOnlyList<OperationsMailboxAddress> For(OperationsCustomer customer, IEnumerable<OperationsWarehouse> warehouses)
    {
        ArgumentNullException.ThrowIfNull(customer);
        ArgumentNullException.ThrowIfNull(warehouses);

        if (customer.Code is null)
        {
            return Array.Empty<OperationsMailboxAddress>();
        }

        // One mailbox per company (owner, 2026-10-21): the warehouse named as the mailbox, or the first one that could be.
        return (Mailbox(warehouses) is { } mailbox ? new[] { mailbox } : Array.Empty<OperationsWarehouse>())
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
