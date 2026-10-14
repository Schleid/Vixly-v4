using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Sales.PreparePlatformOffer;

// =========================================================
// FILE: PreparePlatformOfferRequest.cs
//
// PURPOSE:
// Prepares an offer link with the agreed terms, as a draft (D-S21).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class PreparePlatformOfferRequest
{
    public string CompanyName { get; init; } = string.Empty;

    public string? BoundEmail { get; init; }

    public string Reason { get; init; } = string.Empty;

    public Guid PlatformPlanId { get; init; }

    public PlatformSubscriptionTerm Term { get; init; }

    public int Licences { get; init; }

    public PlatformDiscountTermsInput? Discount { get; init; }

    public decimal? SetupFeeOverride { get; init; }

    public IReadOnlyList<PlatformOfferChargeItem> Charges { get; init; } = Array.Empty<PlatformOfferChargeItem>();

    public bool StartTrial { get; init; }

    public DateTimeOffset ExpiresUtc { get; init; }

    public int UsesAllowed { get; init; } = 1;
}
