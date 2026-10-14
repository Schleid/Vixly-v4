using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformPromotionListFilter.cs
//
// PURPOSE:
// Filters for the promotion list.
//
// RESPONSIBILITIES:
// - Carry one row for a console list
//
// IMPORTANT NOTES:
// - Read model; built by Infrastructure
// =========================================================
public sealed record PlatformPromotionListFilter(
    PlatformPromotionStatus? Status,
    string? Search,
    string Language);
