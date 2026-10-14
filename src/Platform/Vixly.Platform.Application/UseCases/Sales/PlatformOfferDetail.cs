using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Sales;

// =========================================================
// FILE: PlatformOfferDetail.cs
//
// PURPOSE:
// One offer link for its console page.
//
// RESPONSIBILITIES:
// - Carry the agreed terms, the state, the uses and the decision
//
// IMPORTANT NOTES:
// - The link itself is never readable again: it is shown once when issued
// =========================================================
public sealed record PlatformOfferDetail(
    Guid OfferId,
    string CompanyName,
    string? BoundEmail,
    string Reason,
    Guid PlatformPlanId,
    string PlanCode,
    string PlanName,
    string CurrencyCode,
    PlatformSubscriptionTerm Term,
    int Licences,
    PlatformDiscountTermsItem? Discount,
    decimal? SetupFeeOverride,
    IReadOnlyList<PlatformOfferChargeItem> Charges,
    bool StartTrial,
    DateTimeOffset ExpiresUtc,
    int UsesAllowed,
    int Uses,
    PlatformOfferStatus Status,
    string? PreparedByName,
    string? DecidedByName,
    DateTimeOffset? DecidedUtc,
    string? DecisionReason,
    DateTimeOffset? IssuedUtc,
    DateTimeOffset CreatedUtc);
