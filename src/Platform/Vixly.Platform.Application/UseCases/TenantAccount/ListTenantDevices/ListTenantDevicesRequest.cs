using Vixly.Platform.Domain.Licensing;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ListTenantDevices;

// =========================================================
// FILE: ListTenantDevicesRequest.cs
//
// PURPOSE:
// The tenant's devices, for its device page in Operations (call 24).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListTenantDevicesRequest
{
    public PlatformDeviceStatus? Status { get; init; }

    public string? Search { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
