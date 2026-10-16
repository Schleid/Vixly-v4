namespace Vixly.Platform.Domain.Legal;

// =========================================================
// FILE: PlatformLegalDocumentPart.cs
//
// PURPOSE:
// One exact document version a Terms version includes (D-Lg1).
//
// RESPONSIBILITIES:
// - Name the included version by id, kind and label
//
// IMPORTANT NOTES:
// - Owned by the Terms version; copied at draft time, frozen at publish
// =========================================================
public sealed class PlatformLegalDocumentPart
{
    private PlatformLegalDocumentPart()
    {
        Version = string.Empty;
    }

    internal PlatformLegalDocumentPart(Guid documentId, PlatformLegalDocumentKind kind, string version)
    {
        IncludedDocumentId = documentId;
        Kind = kind;
        Version = version;
    }

    public Guid IncludedDocumentId { get; private set; }

    public PlatformLegalDocumentKind Kind { get; private set; }

    public string Version { get; private set; }
}
