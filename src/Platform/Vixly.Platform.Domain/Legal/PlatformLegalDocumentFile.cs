using System.Security.Cryptography;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Legal;

// =========================================================
// FILE: PlatformLegalDocumentFile.cs
//
// PURPOSE:
// The approved PDF of one legal document version in one language (D-Lg2).
//
// RESPONSIBILITIES:
// - Check it is a real PDF within the size limit
// - Make the SHA-256 Platform-side
// - Hold the plain-text summary of what changed
//
// IMPORTANT NOTES:
// - Replaced only while the document is a draft (checked by the use case)
// - Served as a download; never rendered as HTML
// =========================================================
public sealed class PlatformLegalDocumentFile : AuditableEntity
{
    private PlatformLegalDocumentFile()
    {
        Language = string.Empty;
        Content = Array.Empty<byte>();
        Sha256 = string.Empty;
    }

    private PlatformLegalDocumentFile(Guid documentId, string language, DateTimeOffset utcNow)
        : base(utcNow)
    {
        PlatformLegalDocumentId = documentId;
        Language = language;
        Content = Array.Empty<byte>();
        Sha256 = string.Empty;
    }

    public Guid PlatformLegalDocumentId { get; private set; }

    public string Language { get; private set; }

    public byte[] Content { get; private set; }

    public string Sha256 { get; private set; }

    public int SizeBytes { get; private set; }

    public string? Summary { get; private set; }

    public static PlatformLegalDocumentFile Create(Guid documentId, string? language, byte[]? content, string? summary, DateTimeOffset utcNow)
    {
        if (!Vixly.Shared.Kernel.Localization.LanguageCode.TryCreate(language, out Vixly.Shared.Kernel.Localization.LanguageCode? code))
        {
            throw new DomainException(PlatformLegalErrors.LanguagesMissing);
        }

        PlatformLegalDocumentFile file = new(documentId, code!.Value, utcNow);
        file.Replace(content, summary, utcNow);
        return file;
    }

    public void Replace(byte[]? content, string? summary, DateTimeOffset utcNow)
    {
        if (content is null || content.Length < 5 || content.Length > PlatformLegalLimits.MaxFileMegabytes * 1024 * 1024
            || content[0] != (byte)'%' || content[1] != (byte)'P' || content[2] != (byte)'D' || content[3] != (byte)'F' || content[4] != (byte)'-')
        {
            throw new DomainException(PlatformLegalErrors.FileInvalid);
        }

        string? text = string.IsNullOrWhiteSpace(summary) ? null : summary.Trim();

        if (text is { Length: > PlatformLegalLimits.SummaryMaxLength } || (text?.Any(character => char.IsControl(character) && character is not '\n' and not '\r') ?? false))
        {
            throw new DomainException(PlatformLegalErrors.SummaryInvalid);
        }

        Content = content;
        SizeBytes = content.Length;
        Sha256 = Convert.ToHexString(SHA256.HashData(content)).ToLowerInvariant();
        Summary = text;
        MarkAsModified(utcNow);
    }
}
