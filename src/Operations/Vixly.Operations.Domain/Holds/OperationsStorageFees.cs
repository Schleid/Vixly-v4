namespace Vixly.Operations.Domain.Holds;

// =========================================================
// FILE: OperationsStorageFees.cs
//
// PURPOSE:
// The storage fee a package owes for waiting at the warehouse (Slice 4D step 3, Q-D3).
//
// RESPONSIBILITIES:
// - The days past the company's free days, times its fee per day
//
// IMPORTANT NOTES:
// - Shown to staff and the customer; charged with the shipment's other charges (dispatch, Slice 5)
// - Counted in whole days from receipt
// =========================================================
public static class OperationsStorageFees
{
    public static int DaysCharged(DateTimeOffset receivedUtc, DateTimeOffset utcNow, int freeDays) =>
        Math.Max(0, (int)Math.Floor((utcNow - receivedUtc).TotalDays) - Math.Max(0, freeDays));

    public static decimal Accrued(DateTimeOffset receivedUtc, DateTimeOffset utcNow, int freeDays, decimal? feePerDay) =>
        feePerDay is > 0m and { } fee ? decimal.Round(DaysCharged(receivedUtc, utcNow, freeDays) * fee, 2, MidpointRounding.AwayFromZero) : 0m;
}
