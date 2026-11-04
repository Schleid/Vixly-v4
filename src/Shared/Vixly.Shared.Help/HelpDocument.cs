
namespace Vixly.Shared.Help;

// =========================================================
// FILE: HelpDocument.cs
//
// PURPOSE:
// Carries a raw article and its language before catalogue construction.
//
// RESPONSIBILITIES:
// - Implement the shared help foundation of ADR-0023.
//
// IMPORTANT NOTES:
// - Contains no plane dependencies or request identity.
// =========================================================
public sealed record HelpDocument(string Language, string Path, string Markdown);
