namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: PlatformPublicPlan.cs
//
// PURPOSE:
// One plan of the public catalogue.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Prices are before tax, in the plan currency (ISO 4217)
// - Features are stable codes, such as data_export
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformPublicPlan(
    string Code,
    string Name,
    string? Description,
    string CurrencyCode,
    decimal MonthlyPricePerLicence,
    decimal AnnualPricePerLicence,
    decimal SetupFee,
    int MinimumLicences,
    int MaxLicensedDevices,
    int MaxWarehouses,
    IReadOnlyList<string> Features,
    int? TrialDays)
{
    // Shipment cap (owner, 2026-10-29): 0 means no cap; the price of each shipment above it.
    public int MaxShipmentsPerMonth { get; init; }

    public decimal ExtraShipmentPrice { get; init; }
}
