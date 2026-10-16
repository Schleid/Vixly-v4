namespace Vixly.Platform.Domain.Collections;

// =========================================================
// FILE: PlatformCollectionStep.cs
//
// PURPOSE:
// The next step the collections clock should take for a case.
//
// RESPONSIBILITIES:
// - Name the the next step the collections clock should take for a case
//
// IMPORTANT NOTES:
// - Computed, never stored
// - Values are part of message payloads; never renumber
// =========================================================
public enum PlatformCollectionStep
{
    Reminder1 = 1,
    Reminder2 = 2,
    FinalWarning = 3,
    Block = 4
}
