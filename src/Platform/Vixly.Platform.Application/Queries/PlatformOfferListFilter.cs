using Vixly.Platform.Domain.Sales;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformOfferListFilter.cs
//
// PURPOSE:
// Filters for the offer list.
//
// RESPONSIBILITIES:
// - Carry one row for a console list
//
// IMPORTANT NOTES:
// - Read model; built by Infrastructure
// =========================================================
public sealed record PlatformOfferListFilter(
    PlatformOfferStatus? Status,
    string? Search,
    string Language);
