using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Locations;

// =========================================================
// FILE: OperationsBranch.cs
//
// PURPOSE:
// A country or market the company serves (owner, 2026-10-29: branches and roles design, B-1 to B-4).
// The physical places are its warehouses.
//
// RESPONSIBILITIES:
// - Hold the code, name and country
// - Hold the legal identity in that country, printed on documents when set (B-4)
// - Hold an optional main office: address, contact and hours
// - Be shown on the public site or not (only with an office address)
// - Deactivate and reactivate
//
// IMPORTANT NOTES:
// - One country per branch (B-2); the office address, when given, is in that country
// - No origin or destination: the company's routes and its warehouses' roles decide what goes where (B-3)
// - Code unique per tenant (database)
// =========================================================
public sealed class OperationsBranch : OperationsEntity
{
    public const int LegalNameMaxLength = 200;

    public const int TaxNumberMaxLength = 50;

    public const int LegalNotesMaxLength = 500;

    private OperationsBranch()
    {
        Code = string.Empty;
        Name = string.Empty;
        CountryCode = string.Empty;
    }

    private OperationsBranch(Guid tenantId, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        Code = string.Empty;
        Name = string.Empty;
        CountryCode = string.Empty;
    }

    public string Code { get; private set; }

    public string Name { get; private set; }

    public string CountryCode { get; private set; }

    // The main office, when the branch has one.
    public OperationsPostalAddress? Address { get; private set; }

    public string? Phone { get; private set; }

    public string? Email { get; private set; }

    public string? OpeningHours { get; private set; }

    public string? LegalName { get; private set; }

    public string? TaxNumber { get; private set; }

    public string? LegalNotes { get; private set; }

    public OperationsLocationStatus Status { get; private set; }

    public bool ShowOnSite { get; private set; }

    public bool IsPublic => ShowOnSite && Status == OperationsLocationStatus.Active && Address is not null;

    public static OperationsBranch Create(Guid tenantId, OperationsLocationDetails details, string? countryCode, OperationsBranchLegal legal, DateTimeOffset utcNow)
    {
        OperationsBranch branch = new(tenantId, utcNow);
        branch.Apply(details, countryCode, legal);
        return branch;
    }

    public void Update(OperationsLocationDetails details, string? countryCode, OperationsBranchLegal legal, OperationsLocationStatus status, DateTimeOffset utcNow)
    {
        Apply(details, countryCode, legal);
        Status = Enum.IsDefined(status) ? status : OperationsLocationStatus.Active;
        MarkAsModified(utcNow);
    }

    private void Apply(OperationsLocationDetails details, string? countryCode, OperationsBranchLegal legal)
    {
        ArgumentNullException.ThrowIfNull(details);
        ArgumentNullException.ThrowIfNull(legal);

        string country = OperationsCountryCodes.Normalize(countryCode);

        if (details.Address is { } address && address.CountryCode != country)
        {
            throw new DomainException(OperationsLocationErrors.AddressOutsideCountry);
        }

        Code = OperationsLocationText.Code(details.Code);
        Name = OperationsLocationText.Required(details.Name, OperationsLocationLimits.NameMaxLength, OperationsLocationErrors.NameRequired, OperationsLocationErrors.NameTooLong);
        CountryCode = country;
        Address = details.Address;
        Phone = OperationsLocationText.Phone(details.Phone);
        Email = OperationsLocationText.Email(details.Email);
        OpeningHours = OperationsLocationText.Optional(details.OpeningHours, OperationsLocationLimits.HoursMaxLength, OperationsLocationErrors.HoursTooLong);
        LegalName = OperationsLocationText.Optional(legal.LegalName, LegalNameMaxLength, OperationsLocationErrors.LegalNameTooLong);
        TaxNumber = OperationsLocationText.Optional(legal.TaxNumber, TaxNumberMaxLength, OperationsLocationErrors.TaxNumberTooLong);
        LegalNotes = OperationsLocationText.Optional(legal.Notes, LegalNotesMaxLength, OperationsLocationErrors.LegalNotesTooLong);
        ShowOnSite = details.ShowOnSite;
    }
}
