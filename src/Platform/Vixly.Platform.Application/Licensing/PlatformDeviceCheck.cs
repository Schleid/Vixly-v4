using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.Licensing;

// =========================================================
// FILE: PlatformDeviceCheck.cs
//
// PURPOSE:
// The answer to "may this device be used now?" (D-L5, D-L10).
//
// RESPONSIBILITIES:
// - Say allowed or the reason code, the subscription access level, and a new secret when it rotates
//
// IMPORTANT NOTES:
// - Codes only; the caller resolves text
// - Customers are never device-checked
// =========================================================
public sealed record PlatformDeviceCheck(bool Allowed, string? Code, PlatformSubscriptionAccess Access, string? NewSecret);
