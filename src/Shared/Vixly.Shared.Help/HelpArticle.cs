
namespace Vixly.Shared.Help;

// =========================================================
// FILE: HelpArticle.cs
//
// PURPOSE:
// Holds one immutable rendered article and searchable source text.
//
// RESPONSIBILITIES:
// - Implement the shared help foundation of ADR-0023.
//
// IMPORTANT NOTES:
// - Contains no plane dependencies or request identity.
// =========================================================
public sealed record HelpArticle(
    string Language, string SourcePath, HelpFrontMatter Metadata,
    string Body, string Html, string PlainText);
