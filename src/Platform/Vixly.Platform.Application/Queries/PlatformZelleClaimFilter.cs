using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformZelleClaimFilter.cs
//
// PURPOSE:
// Which Zelle reports to list.
//
// RESPONSIBILITIES:
// - Filter by status, tenant and a search on invoice, sender or code
//
// IMPORTANT NOTES:
// - All filters optional
// =========================================================
public sealed record PlatformZelleClaimFilter(PlatformZelleClaimStatus? Status, Guid? PlatformTenantId, string? Search);
