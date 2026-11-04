namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsSiteImageKind.cs
//
// PURPOSE:
// The images of a tenant site.
//
// RESPONSIBILITIES:
// - Name the kinds
//
// IMPORTANT NOTES:
// - Stored as int
// =========================================================
public enum OperationsSiteImageKind
{
    Logo = 0,
    LogoDark = 1,
    Favicon = 2,
    Hero = 3,

    // Owner, 2026-10-28: the logo printed on documents, and its black and white copy for thermal labels (made by Vixly).
    DocumentLogo = 4,

    LabelLogo = 5,

    // Website themes, step 1: the company's own air and ocean cargo photos (Air & Sea theme); illustrations until uploaded.
    Air = 6,

    Sea = 7
}
