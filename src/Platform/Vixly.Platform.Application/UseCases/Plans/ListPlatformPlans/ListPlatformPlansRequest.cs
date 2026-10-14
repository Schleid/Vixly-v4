using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Plans.ListPlatformPlans;

// =========================================================
// FILE: ListPlatformPlansRequest.cs
//
// PURPOSE:
// Pages the plans for the console.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformPlansRequest
{
    public PlatformPlanStatus? Status { get; init; }

    public string? Search { get; init; }

    public string? Language { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
