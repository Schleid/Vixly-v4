using Vixly.Platform.Domain.Collections;

namespace Vixly.Platform.Application.UseCases.Collections.ListPlatformCollectionCases;

// =========================================================
// FILE: ListPlatformCollectionCasesRequest.cs
//
// PURPOSE:
// Lists collections cases, paged, with stage, status, currency and extension filters (D-C14).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformCollectionCasesRequest
{
    public PlatformCollectionCaseStatus? Status { get; init; }

    public PlatformCollectionStage? Stage { get; init; }

    public string? CurrencyCode { get; init; }

    public bool? InExtension { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
