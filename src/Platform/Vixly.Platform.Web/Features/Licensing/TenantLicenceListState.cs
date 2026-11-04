using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Licensing;

// =========================================================
// FILE: TenantLicenceListState.cs
//
// PURPOSE:
// What a tenant's licence list shows in Control (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Carry the tenant's devices and whether they are loaded
//
// IMPORTANT NOTES:
// - Immutable; the presenter replaces it
// =========================================================
public sealed record TenantLicenceListState(IReadOnlyList<PlatformDeviceItem> Devices, bool Loaded, Error? Error)
{
    public static TenantLicenceListState Initial { get; } = new(Array.Empty<PlatformDeviceItem>(), false, null);
}
