namespace Vixly.Shared.Localization;

// =========================================================
// FILE: VixlyEmailAction.cs
//
// PURPOSE:
// The main link of an email, shown as a button (VixlyEmail).
//
// RESPONSIBILITIES:
// - The link, and the label when the text gave one ("Pay online: ...")
//
// IMPORTANT NOTES:
// - Without a label, the email's own button words are used
// =========================================================
public sealed record VixlyEmailAction(string Url, string? Label);
