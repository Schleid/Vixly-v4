using Vixly.Platform.Application.Queries;

namespace Vixly.Platform.Application.UseCases.Licensing;

// =========================================================
// FILE: PlatformDeviceDetail.cs
//
// PURPOSE:
// A device with its history and what the reader may do.
//
// RESPONSIBILITIES:
// - Carry the device page in one read
//
// IMPORTANT NOTES:
// - History is newest first
// =========================================================
public sealed record PlatformDeviceDetail(PlatformDeviceItem Device, IReadOnlyList<PlatformDeviceEventItem> History, PlatformLicensingAbilities Abilities);
