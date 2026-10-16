namespace Vixly.Platform.Domain.Legal;

// =========================================================
// FILE: PlatformTermsReminderStep.cs
//
// PURPOSE:
// Which Terms reminder was sent (D-Lg7).
//
// RESPONSIBILITIES:
// - The publish notice, the days before the deadline, the weeks after it, or a manual one
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// - Automatic steps are sent once per tenant and version
// =========================================================
public enum PlatformTermsReminderStep
{
    Published = 1,
    Before14Days = 2,
    Before7Days = 3,
    Before1Day = 4,
    After1Week = 5,
    After2Weeks = 6,
    After3Weeks = 7,
    After4Weeks = 8,
    Manual = 20
}
