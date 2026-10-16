using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Domain.Billing;

// =========================================================
// FILE: PlatformCompanyProfile.cs
//
// PURPOSE:
// Vixly's own company as printed on invoices, credit notes and receipts (D-B12).
//
// RESPONSIBILITIES:
// - Hold legal and trading name, address, contacts, tax ID, website and logo
//
// IMPORTANT NOTES:
// - One row (key "default"); protected by a row version
// - Moved from Slice 8 by the owner (2026-10-15); the logo is an uploaded PNG or JPEG kept in the database, never fetched from a URL
// =========================================================
public sealed class PlatformCompanyProfile : AuditableEntity
{
    public const string SingletonKey = "default";

    private PlatformCompanyProfile()
    {
        Key = SingletonKey;
        Company = null!;
    }

    private PlatformCompanyProfile(PlatformCompanyDetails company, DateTimeOffset utcNow)
        : base(utcNow)
    {
        Key = SingletonKey;
        Company = company;
    }

    public string Key { get; private set; }

    public PlatformCompanyDetails Company { get; private set; }

    public string? TaxId { get; private set; }

    public string? Website { get; private set; }

    public byte[]? Logo { get; private set; }

    public string? LogoContentType { get; private set; }

    public Guid? UpdatedByUserId { get; private set; }

    // Vixly support contact for tenant emails and Operations' help page (Slice 8, D-Lg15).
    public string? SupportEmail { get; private set; }

    public string? SupportPhone { get; private set; }

    public static PlatformCompanyProfile Create(PlatformCompanyDetails company, string? taxId, string? website, Guid actorUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(company);
        PlatformCompanyProfile profile = new(company, utcNow);
        profile.Update(company, taxId, website, actorUserId, utcNow);

        return profile;
    }

    public void SetSupportContact(string? email, string? phone, DateTimeOffset utcNow)
    {
        string? mail = string.IsNullOrWhiteSpace(email) ? null : email.Trim();
        string? number = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim();

        if ((mail is not null && !Vixly.Shared.Kernel.ValueObjects.EmailAddress.TryCreate(mail, out _))
            || (number is not null && !Vixly.Shared.Kernel.ValueObjects.PhoneNumber.TryCreate(number, out _)))
        {
            throw new DomainException(PlatformBillingErrors.SupportContactInvalid);
        }

        SupportEmail = mail is null ? null : Vixly.Shared.Kernel.ValueObjects.EmailAddress.Create(mail).Value;
        SupportPhone = number is null ? null : Vixly.Shared.Kernel.ValueObjects.PhoneNumber.Create(number).Value;
        MarkAsModified(utcNow);
    }

    public void Update(PlatformCompanyDetails company, string? taxId, string? website, Guid actorUserId, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(company);

        if (actorUserId == Guid.Empty)
        {
            throw new DomainException(PlatformBillingErrors.ActorRequired);
        }

        string? tax = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim();
        string? site = string.IsNullOrWhiteSpace(website) ? null : website.Trim();

        if (tax?.Length > PlatformBillingLimits.TaxIdMaxLength)
        {
            throw new DomainException(PlatformBillingErrors.TaxIdTooLong);
        }

        if (site is not null && (site.Length > PlatformBillingLimits.WebsiteMaxLength
            || !Uri.TryCreate(site, UriKind.Absolute, out Uri? uri) || uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new DomainException(PlatformBillingErrors.ProfileInvalid);
        }

        Company = company;
        TaxId = tax;
        Website = site;
        UpdatedByUserId = actorUserId;
        MarkAsModified(utcNow);
    }

    public void SetLogo(byte[]? content, string? contentType, Guid actorUserId, DateTimeOffset utcNow)
    {
        if (content is null || content.Length == 0)
        {
            Logo = null;
            LogoContentType = null;
        }
        else
        {
            bool png = contentType == "image/png" && content.Length > 8 && content[0] == 0x89 && content[1] == 0x50 && content[2] == 0x4E && content[3] == 0x47;
            bool jpeg = contentType == "image/jpeg" && content.Length > 3 && content[0] == 0xFF && content[1] == 0xD8 && content[2] == 0xFF;

            if ((!png && !jpeg) || content.Length > PlatformBillingLimits.MaxLogoKilobytes * 1024)
            {
                throw new DomainException(PlatformBillingErrors.LogoInvalid);
            }

            Logo = content;
            LogoContentType = contentType;
        }

        UpdatedByUserId = actorUserId;
        MarkAsModified(utcNow);
    }
}
