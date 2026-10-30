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
