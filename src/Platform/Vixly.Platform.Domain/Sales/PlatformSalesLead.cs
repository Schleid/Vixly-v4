using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Platform.Domain.Sales;

// =========================================================
// FILE: PlatformSalesLead.cs
//
// PURPOSE:
// A contact request sent from the public site, followed up by Vixly sales (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Check what the visitor typed
// - Move through New, Contacted and Closed with a staff note
//
// IMPORTANT NOTES:
// - Text is kept as written, in the visitor's language; it is never shown to another visitor
// - Nothing here is trusted: every field is checked and clipped
// =========================================================
public sealed class PlatformSalesLead : AuditableEntity
{
    private PlatformSalesLead()
    {
        CompanyName = string.Empty;
        ContactName = string.Empty;
        Email = string.Empty;
        Language = LanguageCode.Default;
    }

    private PlatformSalesLead(DateTimeOffset utcNow)
        : base(utcNow)
    {
        CompanyName = string.Empty;
        ContactName = string.Empty;
        Email = string.Empty;
        Language = LanguageCode.Default;
        Status = PlatformSalesLeadStatus.New;
        StatusChangedUtc = utcNow;
    }

    public string CompanyName { get; private set; }

    public string ContactName { get; private set; }

    public string Email { get; private set; }

    public string? Phone { get; private set; }

    public string? CountryCode { get; private set; }

    public PlatformSalesLeadKind Kind { get; private set; }

    public string? Message { get; private set; }

    public LanguageCode Language { get; private set; }

    public string? SourcePage { get; private set; }

    public string? ClientIp { get; private set; }

    public PlatformSalesLeadStatus Status { get; private set; }

    public DateTimeOffset StatusChangedUtc { get; private set; }

    public string? StaffNote { get; private set; }

    public Guid? HandledByUserId { get; private set; }

    public static PlatformSalesLead Create(
        string? companyName,
        string? contactName,
        string? email,
        string? phone,
        string? countryCode,
        PlatformSalesLeadKind kind,
        string? message,
        LanguageCode language,
        string? sourcePage,
        string? clientIp,
        DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(language);

        string company = Required(companyName, PlatformSalesLeadLimits.CompanyMaxLength, PlatformSalesLeadErrors.CompanyRequired);
        string name = Required(contactName, PlatformSalesLeadLimits.NameMaxLength, PlatformSalesLeadErrors.NameRequired);

        if (!EmailAddress.TryCreate(email, out EmailAddress? address))
        {
            throw new DomainException(PlatformSalesLeadErrors.EmailInvalid);
        }

        string? phoneText = null;

        if (!string.IsNullOrWhiteSpace(phone))
        {
            if (!PhoneNumber.TryCreate(phone, out PhoneNumber? parsed) || parsed.Value.Length > PlatformSalesLeadLimits.PhoneMaxLength)
            {
                throw new DomainException(PlatformSalesLeadErrors.PhoneInvalid);
            }

            phoneText = parsed.Value;
        }

        string? country = string.IsNullOrWhiteSpace(countryCode) ? null : countryCode.Trim().ToUpperInvariant();

        if (country is not null && !PlatformCountryCodes.All.Contains(country))
        {
            throw new DomainException(PlatformSalesLeadErrors.CountryInvalid);
        }

        if (!Enum.IsDefined(kind))
        {
            throw new DomainException(PlatformSalesLeadErrors.KindInvalid);
        }

        string? text = string.IsNullOrWhiteSpace(message) ? null : message.Trim().Replace("\r\n", "\n", StringComparison.Ordinal);

        if (text is not null && text.Length > PlatformSalesLeadLimits.MessageMaxLength)
        {
            throw new DomainException(PlatformSalesLeadErrors.MessageTooLong);
        }

        return new PlatformSalesLead(utcNow)
        {
            CompanyName = company,
            ContactName = name,
            Email = address.Value,
            Phone = phoneText,
            CountryCode = country,
            Kind = kind,
            Message = text,
            Language = language,
            SourcePage = Clip(sourcePage, PlatformSalesLeadLimits.SourceMaxLength),
            ClientIp = Clip(clientIp, PlatformSalesLeadLimits.IpAddressMaxLength)
        };
    }

    public bool ChangeStatus(PlatformSalesLeadStatus status, string? note, Guid staffUserId, DateTimeOffset utcNow)
    {
        if (!Enum.IsDefined(status))
        {
            throw new DomainException(PlatformSalesLeadErrors.StatusInvalid);
        }

        string? text = string.IsNullOrWhiteSpace(note) ? null : note.Trim();

        if (text is not null && text.Length > PlatformSalesLeadLimits.NoteMaxLength)
        {
            throw new DomainException(PlatformSalesLeadErrors.NoteTooLong);
        }

        if (status == Status && text == StaffNote)
        {
            return false;
        }

        if (status != Status)
        {
            Status = status;
            StatusChangedUtc = utcNow;
        }

        StaffNote = text;
        HandledByUserId = staffUserId;
        MarkAsModified(utcNow);
        return true;
    }

    private static string Required(string? value, int maxLength, Vixly.Shared.Kernel.Errors.Error error)
    {
        string text = value?.Trim() ?? string.Empty;

        if (text.Length == 0 || text.Length > maxLength || text.Any(char.IsControl))
        {
            throw new DomainException(error);
        }

        return text;
    }

    private static string? Clip(string? value, int maxLength)
    {
        string? text = value?.Trim();
        return string.IsNullOrEmpty(text) ? null : text.Length <= maxLength ? text : text[..maxLength];
    }
}
