namespace Vixly.Shared.Hosting.Maintenance;

// =========================================================
// FILE: VixlyMaintenancePageText.cs
//
// PURPOSE:
// The words of the maintenance page, in the visitor's language.
//
// RESPONSIBILITIES:
// - Carry the language, title, body and the message
//
// IMPORTANT NOTES:
// - Each plane fills it from its own resources; tenant planes never say "Vixly" (ADR-0012 W-1)
// =========================================================
public sealed record VixlyMaintenancePageText(string Language, string Title, string Body, string? Message);
