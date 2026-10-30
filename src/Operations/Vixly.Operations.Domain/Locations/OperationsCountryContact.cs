using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.ValueObjects;

namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsCountryContact.cs
//
// PURPOSE:
// How customers in one serving country reach the tenant (Slice 1B).
//
// RESPONSIBILITIES:
// - Hold the country, label, phone, WhatsApp, email, hours and order
//
// IMPORTANT NOTES:
// - Shown under "Contact per country" and in the site JSON-LD
// =========================================================
public sealed class OperationsCountryContact : OperationsEntity
{
    private OperationsCountryContact()
    {
        CountryCode = string.Empty;
        PhonesText = string.Empty;
    }

    private OperationsCountryContact(Guid tenantId, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        CountryCode = string.Empty;
        PhonesText = string.Empty;
    }

    public string CountryCode { get; private set; }

    public string? Label { get; private set; }

    // Every number reaches the same call centre (owner, 2026-10-18); shown in this order.
    public IReadOnlyList<string> Phones => PhonesText.Split(Separator, StringSplitOptions.RemoveEmptyEntries);

    public string? WhatsApp { get; private set; }

    public string? Email { get; private set; }

    public string? Hours { get; private set; }

    public int SortOrder { get; private set; }

    // Stored as one column: international numbers never contain the separator.
    private string PhonesText { get; set; }

    private const char Separator = ';';

    public static OperationsCountryContact Create(Guid tenantId, string? countryCode, string? label, IEnumerable<string?> phones, string? whatsApp, string? email, string? hours, int sortOrder, DateTimeOffset utcNow)
    {
        OperationsCountryContact contact = new(tenantId, utcNow);
        contact.Apply(countryCode, label, phones, whatsApp, email, hours, sortOrder);
        return contact;
    }

    public void Update(string? countryCode, string? label, IEnumerable<string?> phones, string? whatsApp, string? email, string? hours, int sortOrder, DateTimeOffset utcNow)
    {
        Apply(countryCode, label, phones, whatsApp, email, hours, sortOrder);
        MarkAsModified(utcNow);
    }

    private void Apply(string? countryCode, string? label, IEnumerable<string?> phones, string? whatsApp, string? email, string? hours, int sortOrder)
    {
        CountryCode = OperationsCountryCodes.Normalize(countryCode);
        Label = OperationsLocationText.Optional(label, OperationsLocationLimits.LabelMaxLength, OperationsLocationErrors.LabelTooLong);
        PhonesText = string.Join(Separator, Numbers(phones));
        WhatsApp = OperationsLocationText.Phone(whatsApp);
        Email = OperationsLocationText.Email(email);
        Hours = OperationsLocationText.Optional(hours, OperationsLocationLimits.HoursMaxLength, OperationsLocationErrors.HoursTooLong);
        SortOrder = Math.Clamp(sortOrder, 0, 999);
    }

    private static List<string> Numbers(IEnumerable<string?> phones)
    {
        ArgumentNullException.ThrowIfNull(phones);

        List<string> numbers = new();

        foreach (string? phone in phones.Where(phone => !string.IsNullOrWhiteSpace(phone)))
        {
            string number = PhoneNumber.TryCreate(phone, out PhoneNumber? parsed) && parsed.Value.StartsWith('+')
                ? parsed.Value
                : throw new DomainException(OperationsLocationErrors.PhoneNotValid(phone!.Trim()));

            if (!numbers.Contains(number, StringComparer.Ordinal))
            {
                numbers.Add(number);
            }
        }

        return numbers.Count switch
        {
            0 => throw new DomainException(OperationsLocationErrors.PhonesRequired),
            > OperationsLocationLimits.MaxPhonesPerContact => throw new DomainException(OperationsLocationErrors.TooManyPhones),
            _ => numbers
        };
    }
}
