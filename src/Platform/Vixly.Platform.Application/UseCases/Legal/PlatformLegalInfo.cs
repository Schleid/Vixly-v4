using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.UseCases.Subscriptions;

// =========================================================
// FILE: PlatformLegalInfo.cs
//
// PURPOSE:
// The legal part of a tenant's entitlements (owner D-Lg6).
//
// RESPONSIBILITIES:
// - Tell Operations the tenant is read-only until the owner accepts the Terms
//
// IMPORTANT NOTES:
// - Customers are never affected
// =========================================================
public sealed record PlatformLegalInfo(PlatformLegalStatus Status, DateTimeOffset? DeadlineUtc, string? RequiredVersion);
