using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformMerchantAccountItem.cs
//
// PURPOSE:
// The merchant account and the provider configuration as shown on Payments settings.
//
// RESPONSIBILITIES:
// - Carry what the console or public page shows
//
// IMPORTANT NOTES:
// - Plain record; no card data beyond brand, last four and expiry
// =========================================================
public sealed record PlatformMerchantAccountItem(
    PlatformCardProvider Provider,
    PlatformPaymentMode Mode,
    string? PublishableKey,
    IReadOnlyList<string> Currencies,
    bool CardPaymentsEnabled,
    DateTimeOffset? LastCheckedUtc,
    bool LastCheckSucceeded,
    string? LastCheckCode,
    bool ProviderConfigured,
    PlatformPaymentMode? ProviderMode,
    string? ProviderProblemCode,
    bool Ready);
