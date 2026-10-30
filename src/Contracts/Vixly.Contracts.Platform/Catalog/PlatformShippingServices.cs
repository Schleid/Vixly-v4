namespace Vixly.Contracts.Platform.Catalog;

// =========================================================
// FILE: PlatformShippingServices.cs
//
// PURPOSE:
// Vixly's shipping-service catalogue (owner decision D-O15, 2026-10-18).
//
// RESPONSIBILITIES:
// - List the five services in display order
// - Find one by code
//
// IMPORTANT NOTES:
// - Codes are stable wire values: never rename one; add new ones at the end
// - Each tenant chooses which services it offers (Operations)
// =========================================================
public static class PlatformShippingServices
{
    public const string AirExpress = "air_express";

    public const string AirStandard = "air_standard";

    public const string OceanConsolidated = "ocean_consolidated";

    public const string OceanFullContainer = "ocean_full_container";

    public const string Ground = "ground";

    public static IReadOnlyList<PlatformShippingServiceInfo> All { get; } = new[]
    {
        new PlatformShippingServiceInfo(AirExpress, "air", 2, 3, 6000),
        new PlatformShippingServiceInfo(AirStandard, "air", 4, 7, 6000),
        new PlatformShippingServiceInfo(OceanConsolidated, "ocean", 14, 28, 1000),
        new PlatformShippingServiceInfo(OceanFullContainer, "ocean", 14, 35, null),
        new PlatformShippingServiceInfo(Ground, "ground", 3, 7, 5000)
    };

    public static PlatformShippingServiceInfo? Find(string? code) => All.FirstOrDefault(service => service.Code == code);
}
