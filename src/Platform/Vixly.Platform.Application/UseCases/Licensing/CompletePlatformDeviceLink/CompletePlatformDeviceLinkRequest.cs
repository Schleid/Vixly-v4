using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.UseCases.Licensing.CompletePlatformDeviceLink;

// =========================================================
// FILE: CompletePlatformDeviceLinkRequest.cs
//
// PURPOSE:
// Enrols the device that opened a first-device or recovery link; the first device completes the registration and activates the tenant (D-L7, D-L8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CompletePlatformDeviceLinkRequest
{
    public string? Token { get; init; }

    public string? Name { get; init; }

    public PlatformDeviceKind Kind { get; init; }

    public string? PublicKey { get; init; }

    public Guid? ReplaceDeviceId { get; init; }
}
