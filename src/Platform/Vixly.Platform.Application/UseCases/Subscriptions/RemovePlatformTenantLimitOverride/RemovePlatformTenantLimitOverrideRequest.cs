namespace Vixly.Platform.Application.UseCases.Subscriptions.RemovePlatformTenantLimitOverride;

// =========================================================
// FILE: RemovePlatformTenantLimitOverrideRequest.cs
//
// PURPOSE:
// Removes a limit override; the plan value applies again.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RemovePlatformTenantLimitOverrideRequest
{
    public Guid PlatformTenantId { get; init; }

    public Guid OverrideId { get; init; }
}
