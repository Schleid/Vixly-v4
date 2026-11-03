namespace Vixly.Shared.Localization;

// =========================================================
// FILE: VixlyEmailBrand.cs
//
// PURPOSE:
// Who an email comes from, as its designed version shows it (VixlyEmail).
//
// RESPONSIBILITIES:
// - The name and logo in the header, the header and button colours
// - Or a company's own wide logo, shown on white under a band of its colour (owner, 2026-10-25)
//
// IMPORTANT NOTES:
// - Platform mail is Vixly's; Operations mail is the company's and never shows Vixly
// =========================================================
public sealed record VixlyEmailBrand(string Name, string HeaderColor, string ButtonColor, byte[]? LogoPng = null, VixlyEmailLogo? WideLogo = null);
