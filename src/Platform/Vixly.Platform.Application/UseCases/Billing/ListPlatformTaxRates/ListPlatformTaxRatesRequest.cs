namespace Vixly.Platform.Application.UseCases.Billing.ListPlatformTaxRates;

// =========================================================
// FILE: ListPlatformTaxRatesRequest.cs
//
// PURPOSE:
// Lists tax rates by country and region, newest first (D-B1).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformTaxRatesRequest
{
    public string? CountryCode { get; init; }
}
