namespace Vixly.Platform.Domain.Legal;

// =========================================================
// FILE: PlatformLegalDocumentKind.cs
//
// PURPOSE:
// The legal documents Vixly publishes (D-Lg1).
//
// RESPONSIBILITIES:
// - Name each kind of document
//
// IMPORTANT NOTES:
// - Values are stored; never renumber
// - Only Terms is accepted; it includes exact versions of the others
// =========================================================
public enum PlatformLegalDocumentKind
{
    Terms = 1,
    Privacy = 2,
    DataProcessing = 3,
    AcceptableUse = 4,
    ServiceLevel = 5
}
