namespace Vixly.Platform.Web.Features.Collections;

// =========================================================
// FILE: CollectionsSettingsInput.cs
//
// PURPOSE:
// The editable values of the Collections settings form.
//
// RESPONSIBILITIES:
// - Hold what the form binds to
//
// IMPORTANT NOTES:
// - Checked by the domain when saved
// =========================================================
public sealed class CollectionsSettingsInput
{
    public bool Enabled { get; set; }

    public int Reminder1Day { get; set; }

    public int Reminder2Day { get; set; }

    public int FinalWarningDay { get; set; }

    public int BlockDay { get; set; }

    public string? RetryDays { get; set; }

    public int MinimumNoticeDays { get; set; }

    public bool StayReadOnly { get; set; }

    public int BlockedReadOnlyDays { get; set; }

    public int ExtensionMaxDays { get; set; }

    public int ExtensionsPerCase { get; set; }

    public int PendingPaymentReadOnlyDays { get; set; }

    public int LongBlockDays { get; set; }
}
