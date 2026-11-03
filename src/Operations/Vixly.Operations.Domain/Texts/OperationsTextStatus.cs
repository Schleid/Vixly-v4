namespace Vixly.Operations.Domain.Texts;

// =========================================================
// FILE: OperationsTextStatus.cs
//
// PURPOSE:
// What became of a text message (Slice 9).
//
// RESPONSIBILITIES:
// - Name each outcome
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// =========================================================
public enum OperationsTextStatus
{
    // Twilio accepted it.
    Sent = 1,

    // Twilio refused it; the notice went by email instead.
    Failed = 2,

    // The customer replied STOP: notices go by email from now on.
    OptedOut = 3,

    // Twilio reported it delivered (or read).
    Delivered = 4,

    // Twilio accepted it but could not deliver it.
    Undelivered = 5
}
