using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformPlanListFilter.cs
//
// PURPOSE:
// Filters of the plans list.
//
// RESPONSIBILITIES:
// - Carry the status filter, the search text and the reader language
//
// IMPORTANT NOTES:
// - Built by the use case from validated input
// =========================================================
public sealed record PlatformPlanListFilter(PlatformPlanStatus? Status, string? Search, string Language);
