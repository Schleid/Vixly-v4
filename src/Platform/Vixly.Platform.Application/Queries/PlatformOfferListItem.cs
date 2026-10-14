using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Queries;

// =========================================================
// FILE: PlatformOfferListItem.cs
//
// PURPOSE:
// One offer link in the console list.
//
// RESPONSIBILITIES:
// - Carry one row for a console list
//
// IMPORTANT NOTES:
// - Read model; built by Infrastructure
// =========================================================
public sealed record PlatformOfferListItem(
    Guid OfferId,
    string CompanyName,
    string? BoundEmail,
    string PlanCode,
    string PlanName,
    PlatformSubscriptionTerm Term,
    int Licences,
    PlatformDiscountKind? Kind,
    decimal? Value,
    PlatformOfferStatus Status,
    DateTimeOffset ExpiresUtc,
    int UsesAllowed,
    int Uses,
    string? PreparedByName,
    DateTimeOffset CreatedUtc);
