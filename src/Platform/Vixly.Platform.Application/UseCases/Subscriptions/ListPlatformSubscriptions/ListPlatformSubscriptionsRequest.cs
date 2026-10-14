using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions.ListPlatformSubscriptions;

// =========================================================
// FILE: ListPlatformSubscriptionsRequest.cs
//
// PURPOSE:
// Pages every tenant subscription for the subscriptions screen.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformSubscriptionsRequest
{
    public PlatformSubscriptionStatus? Status { get; init; }

    public Guid? PlatformPlanId { get; init; }

    public int? RenewsWithinDays { get; init; }

    public string? Search { get; init; }

    public bool IncludeEnded { get; init; }

    public bool DiscountedOnly { get; init; }

    public string? Language { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
