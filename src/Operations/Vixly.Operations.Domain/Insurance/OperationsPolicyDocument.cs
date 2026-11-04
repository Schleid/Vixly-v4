namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsPolicyDocument.cs
//
// PURPOSE:
// A file kept with a policy: the policy itself, an endorsement, a certificate (Slice 5B).
//
// RESPONSIBILITIES:
// - Carry the file's key in the company's file store, its type, checksum, name and kind
//
// IMPORTANT NOTES:
// - Stored as JSON inside the policy; the file itself in the file store
// =========================================================
public sealed class OperationsPolicyDocument
{
    public const int NameMaxLength = 120;

    private OperationsPolicyDocument()
    {
        Kind = string.Empty;
        FileName = string.Empty;
        Key = string.Empty;
        ContentType = string.Empty;
        Sha256 = string.Empty;
    }

    internal OperationsPolicyDocument(Guid id, string kind, string fileName, string key, string contentType, string sha256, DateTimeOffset uploadedUtc)
    {
        (Id, Kind, FileName, Key, ContentType, Sha256, UploadedUtc) = (id, kind, fileName, key, contentType, sha256, uploadedUtc);
    }

    // The kinds a company files.
    public static IReadOnlyList<string> Kinds { get; } = new[] { "policy", "endorsement", "certificate", "other" };

    public Guid Id { get; private set; }

    public string Kind { get; private set; }

    public string FileName { get; private set; }

    public string Key { get; private set; }

    public string ContentType { get; private set; }

    public string Sha256 { get; private set; }

    public DateTimeOffset UploadedUtc { get; private set; }
}
