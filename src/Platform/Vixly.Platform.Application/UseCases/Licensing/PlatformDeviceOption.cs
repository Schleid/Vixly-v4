namespace Vixly.Platform.Application.UseCases.Licensing;

// =========================================================
// FILE: PlatformDeviceOption.cs
//
// PURPOSE:
// A device holding a seat, as a choice in a form (D-O13).
//
// RESPONSIBILITIES:
// - Carry the id, number and name
//
// IMPORTANT NOTES:
// - Used for "device to replace" in an owner recovery
// =========================================================
public sealed record PlatformDeviceOption(Guid DeviceId, string Number, string Name);
