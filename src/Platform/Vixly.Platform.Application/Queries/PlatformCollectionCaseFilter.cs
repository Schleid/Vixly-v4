using Vixly.Platform.Domain.Collections;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformCollectionCaseFilter.cs
//
// PURPOSE:
// Filters for the collections case list (D-C14).
//
// RESPONSIBILITIES:
// - Carry the chosen filters
//
// IMPORTANT NOTES:
// - Null means any
// =========================================================
public sealed record PlatformCollectionCaseFilter(
    PlatformCollectionCaseStatus? Status,
    PlatformCollectionStage? Stage,
    string? CurrencyCode,
    bool? InExtension,
    string? Search,
    Guid? PlatformTenantId = null);
