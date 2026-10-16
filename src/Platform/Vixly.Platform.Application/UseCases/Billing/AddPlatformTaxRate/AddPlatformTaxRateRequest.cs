namespace Vixly.Platform.Application.UseCases.Billing.AddPlatformTaxRate;

// =========================================================
// FILE: AddPlatformTaxRateRequest.cs
//
// PURPOSE:
// Adds a tax rate for a country or region from a date; earlier rates stay for issued invoices (D-B1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AddPlatformTaxRateRequest
{
    public string? CountryCode { get; init; }

    public string? RegionCode { get; init; }

    public decimal Rate { get; init; }

    public string? Label { get; init; }

    public DateTimeOffset ValidFromUtc { get; init; }
}
