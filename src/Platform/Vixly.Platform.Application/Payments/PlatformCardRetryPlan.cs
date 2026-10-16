using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.Payments;

// =========================================================
// FILE: PlatformCardRetryPlan.cs
//
// PURPOSE:
// The consent profile and card a collections retry will use (D-C9).
//
// RESPONSIBILITIES:
// - Carry what RetryChargeAsync needs
//
// IMPORTANT NOTES:
// - Only built when a retry is allowed
// =========================================================
public sealed record PlatformCardRetryPlan(PlatformTenantCardProfile Profile, PlatformSavedCard Card);
