namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsSiteFont.cs
//
// PURPOSE:
// The curated fonts a company website can use (website themes, step 1).
//
// RESPONSIBILITIES:
// - Name the fonts
//
// IMPORTANT NOTES:
// - Stored as int; Default is the product font (Inter)
// - Every font is self-hosted under wwwroot/fonts/site with its licence (README.md there)
// =========================================================
public enum OperationsSiteFont
{
    Default = 0,
    Manrope = 1,
    PlusJakartaSans = 2,
    DmSans = 3,
    Lora = 4
}
