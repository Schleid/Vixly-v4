using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Settings;

// =========================================================
// FILE: OperationsShippingServiceSetup.cs
//
// PURPOSE:
// A tenant's setup of one of Vixly's shipping services (design section 2, D-O8, D-O15).
//
// RESPONSIBILITIES:
// - Whether the tenant offers the service, its label, expected business days and volumetric divisor
// - The label code printed large on the shipping label: exactly three capital letters A to Z (owner, 2026-10-31)
//
// IMPORTANT NOTES:
// - The service code comes from the Platform catalogue (checked by the use case)
// - The divisor is stored in cm³ per kg whatever units the tenant shows (D-O8)
// =========================================================
public sealed class OperationsShippingServiceSetup : OperationsEntity
{
    public const int CodeMaxLength = 40;

    public const int LabelMaxLength = 60;

    public const int DaysLimit = 365;

    public const decimal MaxDivisor = 1_000_000m;

    public const int LabelCodeLength = 3;

    private OperationsShippingServiceSetup()
    {
        ServiceCode = string.Empty;
    }

    private OperationsShippingServiceSetup(Guid tenantId, string serviceCode, DateTimeOffset createdUtc)
        : base(tenantId, createdUtc)
    {
        ServiceCode = serviceCode;
    }

    public string ServiceCode { get; private set; }

    public bool Offered { get; private set; }

    // Empty: the page and the customer see the service's own name in their language.
    public string? Label { get; private set; }

    public int MinDays { get; private set; }

    public int MaxDays { get; private set; }

    public decimal? VolumetricDivisor { get; private set; }

    // Slice 11 (owner, 2026-10-22): days after arrival at destination for a parcel to be ready (pickup or delivery); none: not measured.
    public int? ReadyTargetDays { get; private set; }

    public const int ReadyTargetDaysLimit = 60;

    public void ChangeReadyTarget(int? days, DateTimeOffset utcNow)
    {
        if (days is < 1 or > ReadyTargetDaysLimit)
        {
            throw new DomainException(Vixly.Operations.Domain.Reports.OperationsReportErrors.RulesInvalid);
        }

        ReadyTargetDays = days;
        MarkAsModified(utcNow);
    }

    // Owner, 2026-10-31: the code warehouse workers sort by (VXA, VXE, VXS, VXG...); none: the catalogue's default.
    public string? LabelCode { get; private set; }

    // Exactly three letters A to Z, stored in capitals; two services may share a code.
    public void ChangeLabelCode(string? labelCode, DateTimeOffset utcNow)
    {
        string code = labelCode?.Trim().ToUpperInvariant() ?? string.Empty;

        if (!IsLabelCode(code))
        {
            throw new DomainException(OperationsShippingServiceErrors.LabelCodeInvalid);
        }

        LabelCode = code;
        MarkAsModified(utcNow);
    }

    public static bool IsLabelCode(string? code) => code is { Length: LabelCodeLength } && code.All(character => character is >= 'A' and <= 'Z');

    public static OperationsShippingServiceSetup Create(Guid tenantId, string serviceCode, DateTimeOffset utcNow)
    {
        string code = serviceCode?.Trim() ?? string.Empty;

        return code.Length is 0 or > CodeMaxLength
            ? throw new DomainException(OperationsShippingServiceErrors.ServiceUnknown)
            : new OperationsShippingServiceSetup(tenantId, code, utcNow);
    }

    public void Change(bool offered, string? label, int minDays, int maxDays, decimal? volumetricDivisor, bool divisorAllowed, DateTimeOffset utcNow)
    {
        string? text = string.IsNullOrWhiteSpace(label) ? null : label.Trim();

        if (text is { Length: > LabelMaxLength } || (text is not null && (text.Contains('<', StringComparison.Ordinal) || text.Contains('>', StringComparison.Ordinal))))
        {
            throw new DomainException(OperationsShippingServiceErrors.LabelTooLong);
        }

        if (minDays < 1 || maxDays < minDays || maxDays > DaysLimit)
        {
            throw new DomainException(OperationsShippingServiceErrors.DaysInvalid);
        }

        if (divisorAllowed ? volumetricDivisor is not ({ } divisor and > 0 and <= MaxDivisor) : volumetricDivisor is not null)
        {
            throw new DomainException(OperationsShippingServiceErrors.DivisorInvalid);
        }

        Offered = offered;
        Label = text;
        MinDays = minDays;
        MaxDays = maxDays;
        VolumetricDivisor = volumetricDivisor is { } value ? decimal.Round(value, 3) : null;
        MarkAsModified(utcNow);
    }
}
