using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Domain.Payments;

// =========================================================
// FILE: PlatformZelleAccount.cs
//
// PURPOSE:
// Vixly's Zelle details shown at checkout: recipient name, email,
// Zelle tag, phone and a QR code image (Slice 5 update).
//
// RESPONSIBILITIES:
// - Hold one set of Zelle details for the whole platform
// - Refuse to turn Zelle on without a recipient and at least one way to send (email, tag or phone)
// - Keep the QR image (PNG or JPEG, size-limited) with a version that changes on every new image
//
// IMPORTANT NOTES:
// - Singleton row (key "default"); row version guards concurrent edits
// - The QR image is served on its own; summaries never carry it
// =========================================================
public sealed class PlatformZelleAccount : AuditableEntity
{
    private PlatformZelleAccount()
    {
        Key = PlatformPaymentsLimits.SingletonKey;
        RecipientName = string.Empty;
    }

    private PlatformZelleAccount(DateTimeOffset utcNow)
        : base(utcNow)
    {
        Key = PlatformPaymentsLimits.SingletonKey;
        RecipientName = string.Empty;
    }

    public string Key { get; private set; }

    public bool Enabled { get; private set; }

    public string RecipientName { get; private set; }

    public string? Email { get; private set; }

    public string? Tag { get; private set; }

    public string? Phone { get; private set; }

    public byte[]? QrCode { get; private set; }

    public string? QrCodeContentType { get; private set; }

    // Changes with every new image so browsers never show an old one.
    public int QrCodeVersion { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    public bool HasQrCode => QrCodeContentType is not null;

    public static PlatformZelleAccount Create(DateTimeOffset utcNow) => new(utcNow);

    public void Update(bool enabled, string? recipientName, string? email, string? tag, string? phone, Guid actorUserId, DateTimeOffset utcNow)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformZelleErrors.ActorRequired);
        }

        string recipient = recipientName?.Trim() ?? string.Empty;

        if (recipient.Length > PlatformZelleLimits.RecipientMaxLength)
        {
            throw new DomainException(PlatformZelleErrors.RecipientInvalid);
        }

        string? mail = Optional(email);

        if (mail is not null)
        {
            mail = EmailAddress.TryCreate(mail, out EmailAddress? parsed) && parsed!.Value.Length <= PlatformZelleLimits.EmailMaxLength
                ? parsed.Value
                : throw new DomainException(PlatformZelleErrors.EmailInvalid);
        }

        string? zelleTag = Optional(tag);

        if (zelleTag is not null && (zelleTag.Length > PlatformZelleLimits.TagMaxLength || zelleTag.Any(char.IsWhiteSpace) || zelleTag.Any(char.IsControl)))
        {
            throw new DomainException(PlatformZelleErrors.TagInvalid);
        }

        string? number = Optional(phone);

        if (number is not null)
        {
            number = PhoneNumber.TryCreate(number, out PhoneNumber? parsed) && parsed!.Value.Length <= PlatformZelleLimits.PhoneMaxLength
                ? parsed.Value
                : throw new DomainException(PlatformZelleErrors.PhoneInvalid);
        }

        if (enabled && (recipient.Length == 0 || (mail is null && zelleTag is null && number is null)))
        {
            throw new DomainException(PlatformZelleErrors.SettingsInvalid);
        }

        Enabled = enabled;
        RecipientName = recipient;
        Email = mail;
        Tag = zelleTag;
        Phone = number;
        UpdatedByUserId = actorUserId;
        MarkAsModified(utcNow);
    }

    // Null or empty content removes the image.
    public void SetQrCode(byte[]? content, string? contentType, Guid actorUserId, DateTimeOffset utcNow)
    {
        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformZelleErrors.ActorRequired);
        }

        if (content is null || content.Length == 0)
        {
            QrCode = null;
            QrCodeContentType = null;
        }
        else
        {
            bool png = contentType == "image/png" && content.Length > 8 && content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47;
            bool jpeg = contentType == "image/jpeg" && content.Length > 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF;

            if ((!png && !jpeg) || content.Length > PlatformZelleLimits.MaxQrKilobytes * 1024)
            {
                throw new DomainException(PlatformZelleErrors.QrCodeInvalid);
            }

            QrCode = content;
            QrCodeContentType = contentType;
        }

        QrCodeVersion++;
        UpdatedByUserId = actorUserId;
        MarkAsModified(utcNow);
    }

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
