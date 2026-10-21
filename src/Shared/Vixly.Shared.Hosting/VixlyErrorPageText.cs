namespace Vixly.Shared.Hosting;

// =========================================================
// FILE: VixlyErrorPageText.cs
//
// PURPOSE:
// The words of the error page, in the visitor's language (ADR-0019 follow-up, 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the language, title, message, link text and link address
//
// IMPORTANT NOTES:
// - Each plane fills it from its own resources; tenant planes never say "Vixly" (ADR-0012 W-1)
// =========================================================
public sealed record VixlyErrorPageText(string Language, string Title, string Body, string Back, string BackUrl);
