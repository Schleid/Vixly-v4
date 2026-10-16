using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.Licensing;

// =========================================================
// FILE: PlatformDeviceEnrolment.cs
//
// PURPOSE:
// What Operations gets back when a device enrols (D-L4, D-L5).
//
// RESPONSIBILITIES:
// - Carry the device id, its number, its state and, for the secret method, the first secret
//
// IMPORTANT NOTES:
// - The secret is returned once and never stored in clear
// =========================================================
public sealed record PlatformDeviceEnrolment(Guid DeviceId, string Number, PlatformDeviceStatus Status, string? Secret);
