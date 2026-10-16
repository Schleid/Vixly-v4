namespace Vixly.Platform.Application.UseCases.Licensing.DecidePlatformDeviceRelease;

// =========================================================
// FILE: DecidePlatformDeviceReleaseRequest.cs
//
// PURPOSE:
// Vixly staff approve or refuse a release over the tenant's allowance (D-L6).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class DecidePlatformDeviceReleaseRequest
{
    public Guid DeviceId { get; init; }

    public bool Approve { get; init; }

    public string? Note { get; init; }
}
