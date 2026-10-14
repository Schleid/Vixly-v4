using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformOneTimeChargeItem.cs
//
// PURPOSE:
// A one-time charge for the tenant page (D-S23).
//
// RESPONSIBILITIES:
// - Carry the description, amount, state, reason and author
//
// IMPORTANT NOTES:
// - Amounts before tax
// =========================================================
public sealed record PlatformOneTimeChargeItem(
    Guid ChargeId,
    string Description,
    decimal Amount,
    string CurrencyCode,
    PlatformOneTimeChargeStatus Status,
    string Reason,
    string? AddedByName,
    DateTimeOffset CreatedUtc);
