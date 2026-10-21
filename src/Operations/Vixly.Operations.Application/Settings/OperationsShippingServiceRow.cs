namespace Vixly.Operations.Application.Settings;

// =========================================================
// FILE: OperationsShippingServiceRow.cs
//
// PURPOSE:
// One catalogue service and the tenant's setup of it, in the tenant's units (Slice 1E).
//
// RESPONSIBILITIES:
// - Carry what the services page shows and posts
//
// IMPORTANT NOTES:
// - Divisor in the tenant's units; null when the service is priced by container
// =========================================================
public sealed record OperationsShippingServiceRow(string Code, string Mode, bool Offered, string? Label, int MinDays, int MaxDays, decimal? VolumetricDivisor, bool DivisorAllowed);
