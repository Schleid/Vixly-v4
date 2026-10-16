using Vixly.Platform.Application.Payments;

namespace Vixly.Platform.Infrastructure.Payments;

// =========================================================
// FILE: SimulatedCheckout.cs
//
// PURPOSE:
// One checkout opened at the simulated provider.
//
// RESPONSIBILITIES:
// - Carry the request and whether it is open, complete or expired
//
// IMPORTANT NOTES:
// - Development only
// =========================================================
public sealed record SimulatedCheckout(string Id, PlatformCheckoutRequest Request, string Status);
