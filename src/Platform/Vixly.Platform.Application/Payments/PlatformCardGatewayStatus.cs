using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCardGatewayStatus.cs
//
// PURPOSE:
// Whether the card provider is configured on this host, and in which mode.
//
// RESPONSIBILITIES:
// - Carry whether the card provider is configured on this host, and in which mode
//
// IMPORTANT NOTES:
// - Read from configuration; never carries a secret (S-11)
// - ProblemCode is a platform.payments.* code when not configured
// =========================================================
public sealed record PlatformCardGatewayStatus(PlatformCardProvider Provider, bool Configured, PlatformPaymentMode? Mode, string? ProblemCode);
