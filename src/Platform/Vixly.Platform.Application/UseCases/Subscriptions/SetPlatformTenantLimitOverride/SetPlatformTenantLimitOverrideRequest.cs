using Vixly.Platform.Domain.Subscriptions;

namespace Vixly.Platform.Application.UseCases.Subscriptions.SetPlatformTenantLimitOverride;

// =========================================================
// FILE: SetPlatformTenantLimitOverrideRequest.cs
//
// PURPOSE:
// Overrides one of a tenant's plan limits (D-S8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SetPlatformTenantLimitOverrideRequest
{
    public Guid PlatformTenantId { get; init; }

    public PlatformPlanLimitKind Kind { get; init; }

    public int Value { get; init; }

    public string Reason { get; init; } = string.Empty;

    public DateTimeOffset? EndsUtc { get; init; }
}
