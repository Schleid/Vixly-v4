using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCardResult.cs
//
// PURPOSE:
// The provider's answer about a charge, refund or checkout.
//
// RESPONSIBILITIES:
// - Carry the provider's answer about a charge, refund or checkout
//
// IMPORTANT NOTES:
// - FailureCode is a short provider code, never provider text (S-9)
// =========================================================
public sealed record PlatformCardResult(string? ProviderId, PlatformCardOutcome Outcome, string? FailureCode);
