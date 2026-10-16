using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformSavedCardItem.cs
//
// PURPOSE:
// A saved card as shown: brand, last four, expiry and whether it is the default.
//
// RESPONSIBILITIES:
// - Carry what the console or public page shows
//
// IMPORTANT NOTES:
// - Plain record; no card data beyond brand, last four and expiry
// =========================================================
public sealed record PlatformSavedCardItem(
    Guid SavedCardId,
    string Brand,
    string Last4,
    int ExpiryMonth,
    int ExpiryYear,
    bool IsDefault,
    bool IsExpired,
    PlatformCardAddedVia AddedVia,
    DateTimeOffset AddedUtc);
