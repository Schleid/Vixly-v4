using Vixly.Operations.Application.Pickup;

namespace Vixly.Operations.Application.Tests;

// =========================================================
// FILE: OperationsPickupRemindersTests.cs
//
// PURPOSE:
// Proves the steps of the pickup reminders (Slice 7 step 2, R-10).
//
// RESPONSIBILITIES:
// - The latest step due from the day the shipment became ready; none after the final notice
//
// IMPORTANT NOTES:
// - Pure; no database
// =========================================================
public sealed class OperationsPickupRemindersTests
{
    private static readonly DateTimeOffset Ready = new(2026, 10, 1, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TheLatestStepDue_IsSent_AndNothingAfterTheFinalNotice()
    {
        Assert.Equal(0, OperationsPickupReminders.DueStep(Ready, 0, Ready.AddDays(6)));
        Assert.Equal(1, OperationsPickupReminders.DueStep(Ready, 0, Ready.AddDays(7)));
        Assert.Equal(0, OperationsPickupReminders.DueStep(Ready, 1, Ready.AddDays(8)));
        Assert.Equal(3, OperationsPickupReminders.DueStep(Ready, 1, Ready.AddDays(22)));
        Assert.Equal(4, OperationsPickupReminders.DueStep(Ready, 1, Ready.AddDays(40)));
        Assert.Equal(0, OperationsPickupReminders.DueStep(Ready, 4, Ready.AddDays(40)));
    }
}
