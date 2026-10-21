using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Tracking;

namespace Vixly.Operations.Application.Holds;

// =========================================================
// FILE: OperationsUnclaimed.cs
//
// PURPOSE:
// Says when a package waits for its customer, and since how long (Slice 3E, Q-R6).
//
// RESPONSIBILITIES:
// - Name the holds that wait for the customer
// - Say whether a package is past the company's unclaimed days
//
// IMPORTANT NOTES:
// - Counted from receipt at the warehouse
// - An unidentified package waits too: it has nobody to remind
// =========================================================
public static class OperationsUnclaimed
{
    public const OperationsHoldReasons CustomerHolds = OperationsHoldReasons.AwaitingCustomerInformation | OperationsHoldReasons.Damaged | OperationsHoldReasons.InspectionDocuments
        | OperationsHoldReasons.InspectionPermit | OperationsHoldReasons.InspectionValue | OperationsHoldReasons.InspectionFee | OperationsHoldReasons.AwaitingShipment;

    public static bool IsWaiting(OperationsParcel parcel)
    {
        ArgumentNullException.ThrowIfNull(parcel);
        return parcel.WarehouseId is not null
            && (parcel.Milestone is null || parcel.Milestone == OperationsMilestone.ReceivedAtOrigin)
            && (parcel.HoldReasons & (CustomerHolds | OperationsHoldReasons.Unidentified)) != OperationsHoldReasons.None;
    }

    public static bool IsUnclaimed(OperationsParcel parcel, int days, DateTimeOffset utcNow) =>
        IsWaiting(parcel) && parcel.CreatedUtc <= utcNow.AddDays(-days);

    // The reminder due now (1 to 4, the 4th the final notice), or 0.
    public static int DueReminder(OperationsParcel parcel, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(parcel);

        if (parcel.CustomerId is null || !IsWaiting(parcel) || parcel.RemindersSent >= OperationsHoldLimits.ReminderDays.Count)
        {
            return 0;
        }

        // The latest step that is due: a job that missed days sends only that one (the final notice alone, if it is due).
        for (int step = OperationsHoldLimits.ReminderDays.Count; step > parcel.RemindersSent; step--)
        {
            if (parcel.CreatedUtc <= utcNow.AddDays(-OperationsHoldLimits.ReminderDays[step - 1]))
            {
                return step;
            }
        }

        return 0;
    }
}
