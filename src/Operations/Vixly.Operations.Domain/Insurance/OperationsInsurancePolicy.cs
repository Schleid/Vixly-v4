using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Insurance;

// =========================================================
// FILE: OperationsInsurancePolicy.cs
//
// PURPOSE:
// One insurance policy the company holds: who insures it, against what, up to how much (Slice 5B, design section 3).
//
// RESPONSIBILITIES:
// - Record and change the policy's terms, checked
// - Keep its documents (the policy, endorsements); archive it
// - Tell whether it is in force on a day and whether it ends soon
//
// IMPORTANT NOTES:
// - Several policies at once is normal; an expired or archived one stays readable
// - A self-insured "policy" has no insurer or number: the company's own funds within its own limits
// =========================================================
public sealed class OperationsInsurancePolicy : OperationsEntity
{
    public const int NameMaxLength = 120;

    public const int ContactMaxLength = 120;

    public const int NumberMaxLength = 60;

    public const int TerritoryMaxLength = 300;

    public const int TextMaxLength = 2000;

    public const int MaxDocuments = 20;

    public const int MaxNoticeDays = 365;

    // A policy ending within this many days is flagged (design section 3).
    public const int EndingSoonDays = 30;

    private readonly List<OperationsPolicyDocument> _documents = new();

    private OperationsInsurancePolicy()
    {
    }

    private OperationsInsurancePolicy(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public OperationsPolicyScope Scope { get; private set; }

    public OperationsPolicyKind Kind { get; private set; }

    public string? InsurerName { get; private set; }

    public string? InsurerEmail { get; private set; }

    public string? InsurerPhone { get; private set; }

    public string? ClaimsEmail { get; private set; }

    public string? ClaimsPhone { get; private set; }

    public string? BrokerName { get; private set; }

    public string? BrokerEmail { get; private set; }

    public string? BrokerPhone { get; private set; }

    public string? PolicyNumber { get; private set; }

    public DateOnly? StartsOn { get; private set; }

    public DateOnly? EndsOn { get; private set; }

    public string CurrencyCode { get; private set; } = "USD";

    public decimal? LimitPerShipment { get; private set; }

    public decimal? LimitPerConveyance { get; private set; }

    public decimal? LimitPerLocation { get; private set; }

    public decimal? LimitPerYear { get; private set; }

    public decimal? DeductibleAmount { get; private set; }

    public decimal? DeductiblePercent { get; private set; }

    public decimal? DeductibleMinimum { get; private set; }

    public OperationsPolicyRisks Risks { get; private set; }

    public OperationsPolicyModes Modes { get; private set; }

    public string? Territory { get; private set; }

    public Vixly.Operations.Domain.Charges.OperationsInsuranceExclusions Exclusions { get; private set; }

    public string? OtherExclusions { get; private set; }

    public int? NoticeDays { get; private set; }

    public OperationsDeclarationFrequency Declarations { get; private set; }

    public string? Notes { get; private set; }

    public bool IsArchived { get; private set; }

    public IReadOnlyList<OperationsPolicyDocument> Documents => _documents;

    // The insurer and number; a self-insured policy has neither (pages name it in the reader's language).
    public string Name => string.Join(" ", new[] { InsurerName, PolicyNumber }.Where(part => !string.IsNullOrEmpty(part)));

    public static OperationsInsurancePolicy Create(Guid tenantId, OperationsPolicyTerms terms, DateTimeOffset utcNow)
    {
        OperationsInsurancePolicy policy = new(tenantId, utcNow);
        policy.Apply(terms);
        return policy;
    }

    public void Change(OperationsPolicyTerms terms, DateTimeOffset utcNow)
    {
        Apply(terms);
        MarkAsModified(utcNow);
    }

    public void Archive(bool archived, DateTimeOffset utcNow)
    {
        IsArchived = archived;
        MarkAsModified(utcNow);
    }

    public OperationsPolicyDocument AttachDocument(string kind, string? fileName, string key, string contentType, string sha256, DateTimeOffset utcNow)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(key);

        if (!OperationsPolicyDocument.Kinds.Contains(kind, StringComparer.Ordinal))
        {
            throw new DomainException(OperationsInsuranceErrors.DocumentInvalid);
        }

        if (_documents.Count >= MaxDocuments)
        {
            throw new DomainException(OperationsInsuranceErrors.TooManyDocuments);
        }

        // The name shown in the list, never a path; the file is served by its id.
        string name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/')) is { Length: > 0 } plain
            ? new string(plain.Where(character => !char.IsControl(character) && character is not ('<' or '>' or '"')).Take(OperationsPolicyDocument.NameMaxLength).ToArray())
            : kind;
        OperationsPolicyDocument document = new(Guid.NewGuid(), kind, name, key, contentType, sha256, utcNow);
        _documents.Add(document);
        MarkAsModified(utcNow);
        return document;
    }

    // The removed document, for the caller to delete its file once saved.
    public OperationsPolicyDocument RemoveDocument(Guid documentId, DateTimeOffset utcNow)
    {
        OperationsPolicyDocument document = _documents.FirstOrDefault(item => item.Id == documentId) ?? throw new DomainException(OperationsInsuranceErrors.NotFound);
        _documents.Remove(document);
        MarkAsModified(utcNow);
        return document;
    }

    public bool IsInForce(DateOnly day) => !IsArchived && (StartsOn is null || StartsOn <= day) && (EndsOn is null || EndsOn >= day);

    public bool EndsSoon(DateOnly day) => IsInForce(day) && EndsOn is { } end && end <= day.AddDays(EndingSoonDays);

    public bool Covers(Vixly.Operations.Domain.Charges.OperationsTransportMode mode) => Modes == OperationsPolicyModes.None || Modes.HasFlag(mode switch
    {
        Vixly.Operations.Domain.Charges.OperationsTransportMode.Ocean => OperationsPolicyModes.Ocean,
        Vixly.Operations.Domain.Charges.OperationsTransportMode.Ground => OperationsPolicyModes.Ground,
        _ => OperationsPolicyModes.Air
    });

    // The kinds that fit each scope (I-6); self-insured and "other" fit both.
    public static bool Fits(OperationsPolicyScope scope, OperationsPolicyKind kind) => scope switch
    {
        OperationsPolicyScope.CompanyOperations => kind is OperationsPolicyKind.FreightForwarderLiability or OperationsPolicyKind.WarehouseLegalLiability
            or OperationsPolicyKind.CarrierLiability or OperationsPolicyKind.GeneralLiability or OperationsPolicyKind.SelfInsured or OperationsPolicyKind.Other,
        OperationsPolicyScope.CustomerShipments => kind is OperationsPolicyKind.OpenCargo or OperationsPolicyKind.ShipmentCargo
            or OperationsPolicyKind.SelfInsured or OperationsPolicyKind.Other,
        _ => false
    };

    private void Apply(OperationsPolicyTerms terms)
    {
        ArgumentNullException.ThrowIfNull(terms);

        if (!Enum.IsDefined(terms.Scope) || !Enum.IsDefined(terms.Kind) || !Fits(terms.Scope, terms.Kind) || !Enum.IsDefined(terms.Declarations))
        {
            throw new DomainException(OperationsInsuranceErrors.KindInvalid);
        }

        bool own = terms.Kind == OperationsPolicyKind.SelfInsured;

        if ((terms.StartsOn is { } start && terms.EndsOn is { } end && end < start) || terms.NoticeDays is < 0 or > MaxNoticeDays)
        {
            throw new DomainException(OperationsInsuranceErrors.DatesInvalid);
        }

        string currency = (terms.CurrencyCode ?? "USD").Trim().ToUpperInvariant();

        if (currency.Length != 3 || !currency.All(char.IsAsciiLetterUpper))
        {
            throw new DomainException(OperationsInsuranceErrors.DetailsInvalid);
        }

        (Scope, Kind) = (terms.Scope, terms.Kind);

        // An insurer and its policy number, except when the company insures itself.
        InsurerName = own ? OperationsInsuranceText.Optional(terms.InsurerName, NameMaxLength) : OperationsInsuranceText.Required(terms.InsurerName, NameMaxLength);
        PolicyNumber = own ? OperationsInsuranceText.Optional(terms.PolicyNumber, NumberMaxLength) : OperationsInsuranceText.Required(terms.PolicyNumber, NumberMaxLength);
        InsurerEmail = OperationsInsuranceText.Optional(terms.InsurerEmail, ContactMaxLength);
        InsurerPhone = OperationsInsuranceText.Optional(terms.InsurerPhone, ContactMaxLength);
        ClaimsEmail = OperationsInsuranceText.Optional(terms.ClaimsEmail, ContactMaxLength);
        ClaimsPhone = OperationsInsuranceText.Optional(terms.ClaimsPhone, ContactMaxLength);
        BrokerName = OperationsInsuranceText.Optional(terms.BrokerName, NameMaxLength);
        BrokerEmail = OperationsInsuranceText.Optional(terms.BrokerEmail, ContactMaxLength);
        BrokerPhone = OperationsInsuranceText.Optional(terms.BrokerPhone, ContactMaxLength);
        (StartsOn, EndsOn, CurrencyCode) = (terms.StartsOn, terms.EndsOn, currency);
        LimitPerShipment = OperationsInsuranceText.Amount(terms.LimitPerShipment);
        LimitPerConveyance = OperationsInsuranceText.Amount(terms.LimitPerConveyance);
        LimitPerLocation = OperationsInsuranceText.Amount(terms.LimitPerLocation);
        LimitPerYear = OperationsInsuranceText.Amount(terms.LimitPerYear);
        DeductibleAmount = OperationsInsuranceText.Amount(terms.DeductibleAmount);
        DeductiblePercent = OperationsInsuranceText.Percent(terms.DeductiblePercent);
        DeductibleMinimum = OperationsInsuranceText.Amount(terms.DeductibleMinimum);
        Risks = terms.Risks & (OperationsPolicyRisks)127;
        Modes = terms.Modes & (OperationsPolicyModes)7;
        Territory = OperationsInsuranceText.Optional(terms.Territory, TerritoryMaxLength);
        Exclusions = terms.Exclusions & Vixly.Operations.Domain.Charges.OperationsInsuranceExclusions.Standard;
        OtherExclusions = OperationsInsuranceText.Optional(terms.OtherExclusions, TextMaxLength, lines: true);
        NoticeDays = terms.NoticeDays;
        Declarations = terms.Scope == OperationsPolicyScope.CustomerShipments ? terms.Declarations : OperationsDeclarationFrequency.None;
        Notes = OperationsInsuranceText.Optional(terms.Notes, TextMaxLength, lines: true);
    }
}
