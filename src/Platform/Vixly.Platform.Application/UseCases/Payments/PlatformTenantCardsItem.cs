using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformTenantCardsItem.cs
//
// PURPOSE:
// A tenant's saved cards and automatic-charging consent.
//
// RESPONSIBILITIES:
// - Carry what the console or public page shows
//
// IMPORTANT NOTES:
// - Plain record; no card data beyond brand, last four and expiry
// =========================================================
public sealed record PlatformTenantCardsItem(
    bool AutoCharge,
    string? ConsentEmail,
    DateTimeOffset? ConsentUtc,
    IReadOnlyList<PlatformSavedCardItem> Cards,
    bool CardPaymentsReady,
    bool CanManage);
