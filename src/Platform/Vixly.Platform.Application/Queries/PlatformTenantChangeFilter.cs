using Vixly.Platform.Domain.Tenants;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformTenantChangeFilter.cs
//
// PURPOSE:
// Filter of the console’s company change queue (B12).
//
// RESPONSIBILITIES:
// - Filter by status and by tenant
//
// IMPORTANT NOTES:
// - Null means any
// =========================================================
public sealed record PlatformTenantChangeFilter(PlatformTenantChangeStatus? Status, Guid? PlatformTenantId);
