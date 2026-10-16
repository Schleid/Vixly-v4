using System.Security.Cryptography;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformBillingDocument.cs
//
// PURPOSE:
// The PDF of an invoice, credit note or receipt, stored once when it is issued (D-B10).
//
// RESPONSIBILITIES:
// - Hold the file, its language, number and SHA-256 hash
//
// IMPORTANT NOTES:
// - Written once, never changed: a download returns exactly what the customer received
// - Kept in the database (no file share on shared hosting)
// =========================================================
public sealed class PlatformBillingDocument : AuditableEntity
{
    private PlatformBillingDocument()
    {
        Number = string.Empty;
        Content = Array.Empty<byte>();
        Sha256 = string.Empty;
        Language = LanguageCode.Default;
    }

    private PlatformBillingDocument(PlatformBillingDocumentKind kind, Guid invoiceId, Guid ownerId, string number, LanguageCode language, byte[] content, DateTimeOffset utcNow)
        : base(utcNow)
    {
        Kind = kind;
        PlatformInvoiceId = invoiceId;
        OwnerId = ownerId;
        Number = number;
        Language = language;
        Content = content;
        Sha256 = Convert.ToHexString(SHA256.HashData(content));
    }

    public PlatformBillingDocumentKind Kind { get; private set; }

    public Guid PlatformInvoiceId { get; private set; }

    public Guid OwnerId { get; private set; }

    public string Number { get; private set; }

    public LanguageCode Language { get; private set; }

    public byte[] Content { get; private set; }

    public string Sha256 { get; private set; }

    public string FileName => Number + ".pdf";

    public static PlatformBillingDocument Create(PlatformBillingDocumentKind kind, Guid invoiceId, Guid ownerId, string number, LanguageCode language, byte[] content, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(language);

        if (content is null || content.Length == 0 || string.IsNullOrWhiteSpace(number))
        {
            throw new DomainException(PlatformBillingErrors.DocumentNotFound);
        }

        return new PlatformBillingDocument(kind, invoiceId, ownerId, number, language, content, utcNow);
    }
}
