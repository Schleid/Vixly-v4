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

    // A sign-in checks without rotating: a lost answer would leave the browser with an old secret (review 2026-10-21).
    public bool Rotate { get; init; } = true;

    // Where the person is now; a device registered before countries were kept takes this one (owner, 2026-10-21).
    public string? Country { get; init; }
}
