namespace Vixly.Platform.Application.UseCases.Licensing.VerifyPlatformDevice;

// =========================================================
// FILE: VerifyPlatformDeviceRequest.cs
//
// PURPOSE:
// Answers whether a device may be used now, from its signature or its rotating secret (D-L5, D-L10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class VerifyPlatformDeviceRequest
{
    public Guid DeviceId { get; init; }

    public string? Secret { get; init; }

    public string? Challenge { get; init; }

    public string? Signature { get; init; }
}
