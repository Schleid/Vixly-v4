namespace Vixly.Platform.Domain.Legal;

// =========================================================
// FILE: PlatformLegalDocumentStatus.cs
//
// PURPOSE:
// Where a legal document version stands (D-Lg3).
//
// RESPONSIBILITIES:
// - Draft, published, or superseded by the next version
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// =========================================================
public enum PlatformLegalDocumentStatus
{
    Draft = 1,
    Published = 2,
    Superseded = 3
}
