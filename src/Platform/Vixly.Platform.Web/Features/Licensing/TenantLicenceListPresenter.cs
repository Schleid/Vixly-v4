using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.UseCases.Licensing.ListPlatformDevices;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Web.Features.Licensing;

// =========================================================
// FILE: TenantLicenceListPresenter.cs
//
// PURPOSE:
// Behaviour of a tenant's licence list in Control (owner, 2026-10-21).
//
// RESPONSIBILITIES:
// - Load every device of the tenant, page after page
//
// IMPORTANT NOTES:
// - Read only; decisions are taken on the device page
// - At most MaxPages pages: a tenant's devices, bound or not, stay far below that
// =========================================================
public sealed class TenantLicenceListPresenter : PlatformPresenter<TenantLicenceListState>
{
    private const int MaxPages = 20;

    private readonly ListPlatformDevicesUseCase _list;

    public TenantLicenceListPresenter(ListPlatformDevicesUseCase list, PlatformUseCaseScope scope, ILogger<TenantLicenceListPresenter> logger)
        : base(scope, TenantLicenceListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(Guid tenantId)
    {
        List<PlatformDeviceItem> devices = new();
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            for (int page = 1; page <= MaxPages; page++)
            {
                ListPlatformDevicesResponse response = await _list.ExecuteAsync(context,
                    new ListPlatformDevicesRequest { PlatformTenantId = tenantId, Page = page, PageSize = PagedRequest.MaximumPageSize }, cancellationToken);

                if (response.Error is not null || response.Devices is not { } result)
                {
                    return response.Error;
                }

                devices.AddRange(result.Items);

                if (page >= result.TotalPages)
                {
                    break;
                }
            }

            return null;
        });

        SetState(error is null ? new TenantLicenceListState(devices, true, null) : State with { Loaded = true, Error = error });
    }
}
