using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformSalesLeadFilter.cs
//
// PURPOSE:
// Filters of the leads list.
//
// RESPONSIBILITIES:
// - Carry the chosen filters
//
// IMPORTANT NOTES:
// - Null means any
// =========================================================
public sealed record PlatformSalesLeadFilter(PlatformSalesLeadStatus? Status, string? Search);
