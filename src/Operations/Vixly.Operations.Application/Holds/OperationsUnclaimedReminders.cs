using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Holds;

// =========================================================
// FILE: OperationsUnclaimedReminders.cs
//
// PURPOSE:
// Sends the reminders of packages waiting for their customer, inside one company (Slice 3E, Q-R6).
//
// RESPONSIBILITIES:
// - Find the packages due a reminder: day 7, 14 and 21, the final notice at day 25
// - Write each email to the outbox and count it on the parcel, in one save
//
// IMPORTANT NOTES:
// - Run by the reminder job for each company; a package gets each reminder once (the row version settles two runs at once)
// - A package more than one step behind gets only the latest reminder
// =========================================================
public sealed class OperationsUnclaimedReminders
{
    private const int Batch = 500;

    private readonly IOperationsHoldRepository _holds;

    private readonly OperationsOutboxWriter _outbox;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public OperationsUnclaimedReminders(IOperationsHoldRepository holds, OperationsOutboxWriter outbox, IOperationsUnitOfWork unitOfWork)
    {
        _holds = holds ?? throw new ArgumentNullException(nameof(holds));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<int> SendAsync(Guid tenantId, string portalSlug, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portalSlug);
        int sent = 0;

        foreach (OperationsParcel parcel in await _holds.ListWaitingAsync(utcNow.AddDays(-OperationsHoldLimits.ReminderDays[0]), Batch, cancellationToken))
        {
            int due = OperationsUnclaimed.DueReminder(parcel, utcNow);

            // Skipped steps (the job did not run for a while) are counted as sent: the customer gets one email, the latest.
            while (due > parcel.RemindersSent + 1 && parcel.Remind(parcel.RemindersSent + 1, utcNow))
            {
            }

            if (due > 0 && parcel.Remind(due, utcNow))
            {
                OperationsCustomerMailKind kind = due == OperationsHoldLimits.ReminderDays.Count ? OperationsCustomerMailKind.ParcelFinalNotice : OperationsCustomerMailKind.ParcelReminder;
                _outbox.Write(OperationsReceivingMessages.ParcelNotice, tenantId, parcel.Id, new OperationsParcelNotice(parcel.CustomerId!.Value, parcel.Id, kind, portalSlug));
                sent++;
            }
        }

        return sent == 0 || await _unitOfWork.SaveChangesAsync(cancellationToken) ? sent : 0;
    }
}
