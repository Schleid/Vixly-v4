using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.UseCases.Licensing.ListPlatformDevices;

// =========================================================
// FILE: ListPlatformDevicesRequest.cs
//
// PURPOSE:
// Lists devices, paged, by tenant, state and search; or the release queue (D-L15).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformDevicesRequest
{
    public Guid? PlatformTenantId { get; init; }

    public PlatformDeviceStatus? Status { get; init; }

    public string? Search { get; init; }

    public bool AwaitingReviewOnly { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
