using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformDeviceFilter.cs
//
// PURPOSE:
// Filters for device lists (D-L15).
//
// RESPONSIBILITIES:
// - Carry the chosen filters
//
// IMPORTANT NOTES:
// - Null means any
// =========================================================
public sealed record PlatformDeviceFilter(Guid? PlatformTenantId, PlatformDeviceStatus? Status, string? Search, bool AwaitingReviewOnly);
