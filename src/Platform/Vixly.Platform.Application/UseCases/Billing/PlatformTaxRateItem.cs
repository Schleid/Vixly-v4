namespace Vixly.Platform.Application.UseCases.Billing;

// =========================================================
// FILE: PlatformTaxRateItem.cs
//
// PURPOSE:
// One tax rate as shown.
//
// RESPONSIBILITIES:
// - Carry what the console shows
//
// IMPORTANT NOTES:
// - Plain record for the console
// =========================================================
public sealed record PlatformTaxRateItem(
    Guid TaxRateId,
    string CountryCode,
    string? RegionCode,
    decimal Rate,
    string Label,
    DateTimeOffset ValidFromUtc,
    bool InForce);
