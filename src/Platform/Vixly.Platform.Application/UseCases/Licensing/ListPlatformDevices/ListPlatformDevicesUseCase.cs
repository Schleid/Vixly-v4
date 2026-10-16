using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.ListPlatformDevices;

// =========================================================
// FILE: ListPlatformDevicesUseCase.cs
//
// PURPOSE:
// Lists devices, paged, by tenant, state and search; or the release queue (D-L15).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ListPlatformDevicesUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public ListPlatformDevicesUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<ListPlatformDevicesResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        ListPlatformDevicesRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _licensing.AuthorizeAsync(context, PlatformPermissions.LicensingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return ListPlatformDevicesResponse.Failure(refused);
        }

        return ListPlatformDevicesResponse.Success(await _licensing.Queries.ListDevicesAsync(
            new PlatformDeviceFilter(request.PlatformTenantId, request.Status, string.IsNullOrWhiteSpace(request.Search) ? null : request.Search.Trim(), request.AwaitingReviewOnly),
            Vixly.Shared.Kernel.Paging.PagedRequest.Create(request.Page, request.PageSize),
            _licensing.UtcNow,
            cancellationToken));
    }
}
