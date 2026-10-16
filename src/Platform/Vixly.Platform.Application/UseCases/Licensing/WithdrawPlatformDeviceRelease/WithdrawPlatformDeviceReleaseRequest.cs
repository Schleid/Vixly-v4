using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.UseCases.Licensing.WithdrawPlatformDeviceRelease;

// =========================================================
// FILE: WithdrawPlatformDeviceReleaseRequest.cs
//
// PURPOSE:
// A tenant admin withdraws a release still waiting for Vixly (D-L6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class WithdrawPlatformDeviceReleaseRequest
{
    public Guid DeviceId { get; init; }
}
