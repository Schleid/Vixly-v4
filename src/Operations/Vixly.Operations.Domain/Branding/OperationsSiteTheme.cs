namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsSiteTheme.cs
//
// PURPOSE:
// The layouts a company website can use (website themes, step 1).
//
// RESPONSIBILITIES:
// - Name the themes
//
// IMPORTANT NOTES:
// - Stored as int; Classic is today's page and every company's default
// - Themes belong to the product, never to one company; each shows the same real content, arranged differently
// =========================================================
public enum OperationsSiteTheme
{
    Classic = 0,

    // Dark hero with a large headline, route illustration, alternating dark and light sections.
    Horizon = 1,

    // Bright split hero with the company's own cargo photo, air and ocean side by side, locations prominent.
    AirSea = 2
}
