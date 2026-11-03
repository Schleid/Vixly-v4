namespace Vixly.Shared.Localization;

// =========================================================
// FILE: VixlyEmailWords.cs
//
// PURPOSE:
// The layout's own words, in the recipient's language (VixlyEmail).
//
// RESPONSIBILITIES:
// - The default button words, the copy-the-link line and the footer lines
//
// IMPORTANT NOTES:
// - Each plane gives them from its own resources (email.layout.*)
// =========================================================
public sealed record VixlyEmailWords(string Button, string CopyLink, IReadOnlyList<string> Footer);
