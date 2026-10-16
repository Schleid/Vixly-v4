namespace Vixly.Platform.Domain.Collections;

// =========================================================
// FILE: PlatformCollectionStage.cs
//
// PURPOSE:
// The furthest step a collections case has reached (D-C3).
//
// RESPONSIBILITIES:
// - Name the the furthest step a collections case has reached (D-C3)
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformCollectionStage
{
    Opened = 1,
    Reminder1 = 2,
    Reminder2 = 3,
    FinalWarning = 4,
    Blocked = 5
}
