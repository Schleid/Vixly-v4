namespace Vixly.Operations.Application.Branding;

// =========================================================
// FILE: OperationsEdgeBrand.cs
//
// PURPOSE:
// A company's brand as the Cloudflare Worker keeps it, to show on its maintenance page (W-M2).
//
// RESPONSIBILITIES:
// - Carry the portal address name (slug), the display name, the main colour, the default language and the logo
//
// IMPORTANT NOTES:
// - The logo is the published site logo, sent only when it is 100 KB or less; otherwise the page shows the name
// =========================================================
public sealed record OperationsEdgeBrand(
    string Slug,
    string Name,
    string? Colour,
    string Language,
    byte[]? Logo,
    string? LogoContentType);
