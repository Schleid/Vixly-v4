using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Licensing;

// =========================================================
// FILE: DevicesOverviewState.cs
//
// PURPOSE:
// What the device list shows.
//
// RESPONSIBILITIES:
// - Hold one page of devices and the paging numbers
//
// IMPORTANT NOTES:
// - Immutable; a new state per change
// =========================================================
public sealed record DevicesOverviewState(IReadOnlyList<PlatformDeviceItem> Items, int Page, int TotalPages, int TotalCount, bool Loaded, Error? Error)
{
    public static DevicesOverviewState Initial { get; } = new(Array.Empty<PlatformDeviceItem>(), 1, 0, 0, false, null);
}
