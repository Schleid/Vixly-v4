namespace Vixly.Operations.Domain.Texts;

// =========================================================
// FILE: OperationsNoticeChannel.cs
//
// PURPOSE:
// How a customer wants the company’s notices (Slice 9; owner, 2026-10-22: the customer picks the channel).
//
// RESPONSIBILITIES:
// - Name each channel
//
// IMPORTANT NOTES:
// - Stored as numbers; never renumber
// - Account emails (verification, password, statements) always go by email
// =========================================================
public enum OperationsNoticeChannel
{
    Email = 0,
    Sms = 1,
    WhatsApp = 2
}
