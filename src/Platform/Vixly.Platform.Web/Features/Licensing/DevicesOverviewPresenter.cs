using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Licensing.ListPlatformDevices;
using Vixly.Platform.Domain.Licensing;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Licensing;

// =========================================================
// FILE: DevicesOverviewPresenter.cs
//
// PURPOSE:
// Behaviour of the device list and the release review queue (Slice 7).
//
// RESPONSIBILITIES:
// - Load one page of devices with the chosen filters
//
// IMPORTANT NOTES:
// - Read only; decisions are taken on the device page
// =========================================================
public sealed class DevicesOverviewPresenter : PlatformPresenter<DevicesOverviewState>
{
    private readonly ListPlatformDevicesUseCase _list;

    public DevicesOverviewPresenter(ListPlatformDevicesUseCase list, PlatformUseCaseScope scope, ILogger<DevicesOverviewPresenter> logger)
        : base(scope, DevicesOverviewState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(Guid? tenantId, PlatformDeviceStatus? status, bool awaitingReview, string? search, int page)
    {
        ListPlatformDevicesResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformDevicesRequest
            {
                PlatformTenantId = tenantId, Status = status, AwaitingReviewOnly = awaitingReview, Search = search, Page = page
            }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Devices is { } result
            ? new DevicesOverviewState(result.Items, result.Page, result.TotalPages, result.TotalCount, true, null)
            : State with { Loaded = true, Error = error });
    }
}
