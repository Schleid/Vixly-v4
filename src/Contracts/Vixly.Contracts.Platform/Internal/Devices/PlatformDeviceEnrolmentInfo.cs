namespace Vixly.Contracts.Platform.Internal.Devices;

// =========================================================
// FILE: PlatformDeviceEnrolmentInfo.cs
//
// PURPOSE:
// The device just enrolled.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Platform.Api and Operations or Edge
//
// IMPORTANT NOTES:
// - Secret is shown once: keep it in the device only
// - Wire type: BCL only (C-0); never a domain type
// - Version 1: add fields only; consumers ignore unknown fields (D-K4)
// =========================================================
public sealed record PlatformDeviceEnrolmentInfo(Guid DeviceId, string Number, string Status, string? Secret);
