using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;

namespace Vixly.Operations.Application.Pickup;

// =========================================================
// FILE: OperationsPickupCodeMail.cs
//
// PURPOSE:
// Email the customer their pickup code (Slice 7 step 2, R-5).
//
// RESPONSIBILITIES:
// - Word the shipment, the place and the code, and link to their shipments
//
// IMPORTANT NOTES:
// - Sent straight away: the code is never written to the database or the outbox
// =========================================================
public static class OperationsPickupCodeMail
{
    public static Task SendAsync(OperationsCustomerAccounts accounts, OperationsCustomer customer, string portalSlug, string shipmentNumber, string place, string code,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(accounts);
        ArgumentNullException.ThrowIfNull(customer);
        return accounts.SendAsync(customer, OperationsCustomerMailKind.PickupCode, accounts.PortalPage(portalSlug, "/customer/shipments"),
            new Dictionary<string, string>(StringComparer.Ordinal) { ["shipment"] = shipmentNumber, ["place"] = place, ["code"] = code },
            new Dictionary<string, string>(StringComparer.Ordinal), cancellationToken);
    }
}
