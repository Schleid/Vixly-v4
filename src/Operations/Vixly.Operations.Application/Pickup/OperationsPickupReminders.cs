using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Shipments;

namespace Vixly.Operations.Application.Pickup;

// =========================================================
// FILE: OperationsPickupReminders.cs
//
// PURPOSE:
// Remind customers of a shipment waiting at its pickup counter, then a final notice (Slice 7 step 2, R-10).
//
// RESPONSIBILITIES:
// - One email per shipment at each step, the days counted from when its first parcel became ready
//
// IMPORTANT NOTES:
// - Only when the company charges storage at the destination (off by default)
// - The same steps as at the origin (7, 14, 21 and 25 days); a job that missed days sends only the latest
// - The steps are kept on the shipment, and only shipments with a step due are read (review)
// =========================================================
public sealed class OperationsPickupReminders
{
    private const int Batch = 500;

    private readonly IOperationsPickupRepository _pickups;
    private readonly IOperationsSettingsRepository _settings;
    private readonly IOperationsReceivingRepository _receiving;
    private readonly OperationsOutboxWriter _outbox;
    private readonly IOperationsUnitOfWork _unitOfWork;

    public OperationsPickupReminders(IOperationsPickupRepository pickups, IOperationsSettingsRepository settings, IOperationsReceivingRepository receiving, OperationsOutboxWriter outbox,
        IOperationsUnitOfWork unitOfWork)
    {
        _pickups = pickups ?? throw new ArgumentNullException(nameof(pickups));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    // The latest step due for a shipment ready since that time; 0 when none is.
    public static int DueStep(DateTimeOffset readySince, int sent, DateTimeOffset utcNow)
    {
        for (int step = OperationsHoldLimits.ReminderDays.Count; step > sent; step--)
        {
            if (readySince <= utcNow.AddDays(-OperationsHoldLimits.ReminderDays[step - 1]))
            {
                return step;
            }
        }

        return 0;
    }

    public async Task<int> SendAsync(Guid tenantId, string portalSlug, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portalSlug);

        if (await _settings.FindAsync(cancellationToken) is not { DestinationStorage: true })
        {
            return 0;
        }

        int sent = 0;

        foreach (OperationsShipment shipment in await _pickups.ListRemindableAsync(utcNow, Batch, cancellationToken))
        {
            int due = DueStep(shipment.PickupReadyUtc!.Value, shipment.PickupRemindersSent, utcNow);

            // Where its parcels wait: the warehouse of one still ready.
            if (due == 0 || (await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken))
                    .FirstOrDefault(parcel => parcel.Milestone == Vixly.Operations.Domain.Tracking.OperationsMilestone.ReadyForPickup) is not { WarehouseId: { } warehouse })
            {
                continue;
            }

            // Skipped steps count as sent: the customer gets one email, the latest.
            while (shipment.PickupRemindersSent < due && shipment.PickupRemind(shipment.PickupRemindersSent + 1, utcNow))
            {
            }

            _outbox.Write(OperationsPickupReminderNotice.MessageType, tenantId, shipment.Id, new OperationsPickupReminderNotice(shipment.CustomerId, portalSlug, shipment.Number ?? string.Empty,
                warehouse, due == OperationsHoldLimits.ReminderDays.Count));
            sent++;
        }

        return sent == 0 || await _unitOfWork.SaveChangesAsync(cancellationToken) ? sent : 0;
    }
}
