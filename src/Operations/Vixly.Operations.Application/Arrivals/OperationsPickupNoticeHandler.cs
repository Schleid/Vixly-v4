using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Messaging;

namespace Vixly.Operations.Application.Arrivals;

// =========================================================
// FILE: OperationsPickupNoticeHandler.cs
//
// PURPOSE:
// Sends the customer the ready-for-pickup email (Slice 7, R-1).
//
// RESPONSIBILITIES:
// - Name the place, its address and hours, and link to their shipments
//
// IMPORTANT NOTES:
// - A customer no longer active gets nothing
// - Sent once per shipment, and only when every parcel still going is ready (the shipment keeps that it was told; review)
// =========================================================
public sealed class OperationsPickupNoticeHandler : IOperationsOutboxHandler
{
    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly TimeProvider _timeProvider;

    private readonly IOperationsSettingsRepository _settings;

    private readonly Vixly.Operations.Application.Pickup.OperationsPickupCodes _codes;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public OperationsPickupNoticeHandler(IOperationsCustomerRepository customers, OperationsCustomerAccounts accounts, IOperationsReceivingRepository receiving, TimeProvider timeProvider,
        IOperationsSettingsRepository settings, Vixly.Operations.Application.Pickup.OperationsPickupCodes codes, IOperationsUnitOfWork unitOfWork)
    {
        _codes = codes ?? throw new ArgumentNullException(nameof(codes));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public string MessageType => OperationsPickupNotice.MessageType;

    public async Task HandleAsync(OperationsOutboxMessage message, CancellationToken cancellationToken)
    {
        OperationsPickupNotice notice = OperationsOutboxWriter.Read<OperationsPickupNotice>(message);

        if (string.IsNullOrWhiteSpace(notice.PortalSlug)
            || await _customers.GetAsync(notice.CustomerId, cancellationToken) is not { Status: OperationsCustomerStatus.Active } customer
            || await _receiving.GetShipmentAsync(message.StreamId, cancellationToken) is not { } shipment)
        {
            return;
        }

        // Every parcel still going is ready; the dispatcher saves the mark with the message.
        if (!(await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken)).All(parcel =>
                Vixly.Operations.Application.Events.OperationsEventDesk.Finished(parcel) || parcel.Milestone >= Vixly.Operations.Domain.Tracking.OperationsMilestone.ReadyForPickup)
            || shipment.PickupNotifiedUtc is not null)
        {
            return;
        }

        Dictionary<string, string> extra = new(StringComparer.Ordinal)
        {
            ["shipment"] = notice.ShipmentNumber,
            ["place"] = notice.Place,
            ["address"] = notice.Address ?? string.Empty,
            ["hours"] = notice.OpeningHours ?? string.Empty
        };

        // Slice 7 step 2 (R-5, review): the pickup code when the company asks for one: the one still valid, or a new one saved before it is sent,
        // so a retry sends the same code.
        string? code = null;

        if ((await _settings.FindAsync(cancellationToken))?.PickupProofs.HasFlag(Vixly.Operations.Domain.Pickup.OperationsPickupProofs.Code) == true)
        {
            code = _codes.Current(shipment);

            if (code is null)
            {
                code = _codes.Issue(shipment);

                if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
                {
                    throw new InvalidOperationException("The pickup code of " + shipment.Id + " could not be saved; the notice is tried again.");
                }
            }
        }

        await _accounts.SendAsync(customer, OperationsPickupNotice.Kind, _accounts.PortalPage(notice.PortalSlug, "/customer/shipments"), extra,
            new Dictionary<string, string>(StringComparer.Ordinal), cancellationToken);

        if (code is not null)
        {
            await Vixly.Operations.Application.Pickup.OperationsPickupCodeMail.SendAsync(_accounts, customer, notice.PortalSlug, notice.ShipmentNumber, notice.Place, code, cancellationToken);
        }

        // Told: the dispatcher saves the mark with the message.
        shipment.NotePickupNotified(_timeProvider.GetUtcNow());
    }
}
