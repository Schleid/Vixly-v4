using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Pickup;

// =========================================================
// FILE: OperationsPickupReminderHandler.cs
//
// PURPOSE:
// Sends the reminder or the final notice of a shipment waiting at its pickup counter (Slice 7 step 2, R-10).
//
// RESPONSIBILITIES:
// - Name the place, its hours and the storage terms, and link to their shipments
//
// IMPORTANT NOTES:
// - A customer no longer active, or a shipment collected since, gets nothing
// =========================================================
public sealed class OperationsPickupReminderHandler : IOperationsOutboxHandler
{
    private readonly IOperationsCustomerRepository _customers;
    private readonly OperationsCustomerAccounts _accounts;
    private readonly IOperationsReceivingRepository _receiving;
    private readonly IOperationsLocationRepository _locations;

    public OperationsPickupReminderHandler(IOperationsCustomerRepository customers, OperationsCustomerAccounts accounts, IOperationsReceivingRepository receiving,
        IOperationsLocationRepository locations)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
    }

    public string MessageType => OperationsPickupReminderNotice.MessageType;

    public async Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
    {
        OperationsPickupReminderNotice notice = OperationsOutboxWriter.Read<OperationsPickupReminderNotice>(message);

        if (string.IsNullOrWhiteSpace(notice.PortalSlug)
            || await _customers.GetAsync(notice.CustomerId, cancellationToken) is not { Status: OperationsCustomerStatus.Active } customer
            || await _receiving.GetShipmentAsync(message.StreamId, cancellationToken) is not { PickedUpUtc: null }
            || await _locations.GetWarehouseAsync(notice.WarehouseId, cancellationToken) is not { } warehouse)
        {
            return;
        }

        await _accounts.SendAsync(customer, notice.Final ? OperationsCustomerMailKind.PickupFinalNotice : OperationsCustomerMailKind.PickupReminder,
            _accounts.PortalPage(notice.PortalSlug, "/customer/shipments"),
            new Dictionary<string, string>(StringComparer.Ordinal) { ["shipment"] = notice.ShipmentNumber, ["place"] = warehouse.Name, ["hours"] = warehouse.OpeningHours ?? string.Empty },
            new Dictionary<string, string>(StringComparer.Ordinal), cancellationToken);
    }
}
