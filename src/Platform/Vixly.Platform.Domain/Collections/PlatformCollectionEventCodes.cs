namespace Vixly.Platform.Domain.Collections;

// =========================================================
// FILE: PlatformCollectionEventCodes.cs
//
// PURPOSE:
// The history codes of a collections case (D-C15).
//
// RESPONSIBILITIES:
// - Name every step a case can record
//
// IMPORTANT NOTES:
// - Each code has en, fr and es text; never reword one that shipped
// =========================================================
public static class PlatformCollectionEventCodes
{
    public const string Opened = "platform.collection_history.opened";
    public const string Reminder1 = "platform.collection_history.reminder_1";
    public const string Reminder2 = "platform.collection_history.reminder_2";
    public const string FinalWarning = "platform.collection_history.final_warning";
    public const string Blocked = "platform.collection_history.blocked";
    public const string NoAccess = "platform.collection_history.no_access";
    public const string CardRetry = "platform.collection_history.card_retry";
    public const string ExtensionGranted = "platform.collection_history.extension_granted";
    public const string ExtensionEnded = "platform.collection_history.extension_ended";
    public const string Note = "platform.collection_history.note";
    public const string Checked = "platform.collection_history.checked";
    public const string Unblocked = "platform.collection_history.unblocked";
    public const string Closed = "platform.collection_history.closed";

    public static IReadOnlyList<string> All { get; } = new[]
    {
        Opened, Reminder1, Reminder2, FinalWarning, Blocked, NoAccess, CardRetry, ExtensionGranted, ExtensionEnded, Note, Checked, Unblocked, Closed
    };
}
