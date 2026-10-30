namespace Vixly.Contracts.Platform.Catalog;

// =========================================================
// FILE: PlatformShippingServiceInfo.cs
//
// PURPOSE:
// One shipping service Vixly offers tenants (owner decision D-O15).
//
// RESPONSIBILITIES:
// - Carry the code, the mode and the starting values a tenant may change
//
// IMPORTANT NOTES:
// - Volumetric divisor in cm³ per kg; null when the service is priced by container
// =========================================================
public sealed record PlatformShippingServiceInfo(string Code, string Mode, int DefaultMinDays, int DefaultMaxDays, int? DefaultVolumetricDivisor);
