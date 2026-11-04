namespace Vixly.Operations.Domain.Holds;

// =========================================================
// FILE: OperationsHoldLimits.cs
//
// PURPOSE:
// Sizes and timing of hold answers and unclaimed packages (Slice 3E).
//
// RESPONSIBILITIES:
// - Keep the columns, the forms and the reminder days in step
//
// IMPORTANT NOTES:
// - Changing a size needs a migration
// =========================================================
public static class OperationsHoldLimits
{
    public const int MaxDocumentBytes = 10 * 1024 * 1024;

    public const int FileNameMaxLength = 120;

    public const int NoteMaxLength = 500;

    public const int StorageKeyMaxLength = 200;

    public const int DefaultUnclaimedDays = 30;

    public const int MinUnclaimedDays = 30;

    public const int MaxUnclaimedDays = 180;

    // Reminders at day 7, 14 and 21 after receipt, the final notice at day 25 (Q-R6).
    public static IReadOnlyList<int> ReminderDays { get; } = new[] { 7, 14, 21, 25 };
}
