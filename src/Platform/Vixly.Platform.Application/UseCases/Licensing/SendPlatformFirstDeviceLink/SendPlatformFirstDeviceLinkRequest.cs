namespace Vixly.Platform.Application.UseCases.Licensing.SendPlatformFirstDeviceLink;

// =========================================================
// FILE: SendPlatformFirstDeviceLinkRequest.cs
//
// PURPOSE:
// Vixly staff email the owner a first-device link (tenants created by staff, D-L7).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class SendPlatformFirstDeviceLinkRequest
{
    public Guid PlatformTenantId { get; init; }
}
