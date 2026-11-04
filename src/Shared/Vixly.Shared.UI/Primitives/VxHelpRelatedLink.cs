namespace Vixly.Shared.UI.Primitives;

// =========================================================
// FILE: VxHelpRelatedLink.cs
//
// PURPOSE:
// Defines or verifies the shared help foundation.
//
// RESPONSIBILITIES:
// - Support the help contract of ADR-0023.
//
// IMPORTANT NOTES:
// - No plane implementation dependencies.
// =========================================================
public sealed record VxHelpRelatedLink(string Title, string Href);
