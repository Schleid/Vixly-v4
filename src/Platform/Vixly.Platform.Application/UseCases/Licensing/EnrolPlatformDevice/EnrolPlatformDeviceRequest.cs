using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.UseCases.Licensing.EnrolPlatformDevice;

// =========================================================
// FILE: EnrolPlatformDeviceRequest.cs
//
// PURPOSE:
// A tenant device asks to join: it waits for a tenant admin, holding no seat (D-L4, D-L5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class EnrolPlatformDeviceRequest
{
    public string? Name { get; init; }

    public PlatformDeviceKind Kind { get; init; }

    public string? PublicKey { get; init; }

    // Where the person registering is, as the tenant's server saw them (owner, 2026-10-21).
    public PlatformDeviceOrigin? Origin { get; init; }
}
