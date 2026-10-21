namespace Vixly.Operations.Application.Ports;

// =========================================================
// FILE: OperationsPlanFeatures.cs
//
// PURPOSE:
// What the tenant's plan lets its site show (W-4, owner 2026-10-18).
//
// RESPONSIBILITIES:
// - Carry the two branding features of the plan
//
// IMPORTANT NOTES:
// - portal_theme_customization: colours and theme; homepage_content_customization: the site texts and main image
// - Saved choices are kept when a feature is missing; the site falls back to the defaults
// =========================================================
public sealed record OperationsPlanFeatures(bool ThemeCustomization, bool ContentCustomization)
{
    public const string Theme = "portal_theme_customization";

    public const string Content = "homepage_content_customization";

    public static OperationsPlanFeatures All { get; } = new(true, true);

    public bool Limited => !ThemeCustomization || !ContentCustomization;
}
