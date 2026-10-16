using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.TenantAccount.ListTenantDevices;

// =========================================================
// FILE: ListTenantDevicesUseCase.cs
//
// PURPOSE:
// The tenant's devices, for its device page in Operations (call 24).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Called by Operations over the internal channel for the tenant in the service token only (S-13, D-K8)
// - Any tenant role
// =========================================================
public sealed class ListTenantDevicesUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public ListTenantDevicesUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<ListTenantDevicesResponse> ExecuteAsync(
        PlatformTenantAuthorizationContext context,
        ListTenantDevicesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        return ListTenantDevicesResponse.Success(await _licensing.Queries.ListDevicesAsync(
            new PlatformDeviceFilter(context.PlatformTenantId, request.Status, string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(), false),
            PagedRequest.Create(request.Page, PlatformTenantAccountLimits.PageSize(request.PageSize)),
            _licensing.UtcNow,
            cancellationToken));
    }
}
