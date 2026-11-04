using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Maintenance.ListPlatformMaintenanceWindows;
using Vixly.Platform.Application.UseCases.Maintenance.SchedulePlatformMaintenanceWindow;
using Vixly.Platform.Application.UseCases.Maintenance.StopPlatformMaintenanceWindow;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Maintenance;

// =========================================================
// FILE: MaintenancePresenter.cs
//
// PURPOSE:
// Loads, publishes and stops maintenance windows (Control backlog 2026-10-22, section 1).
//
// RESPONSIBILITIES:
// - Run the maintenance use cases for its component and hold the state
//
// IMPORTANT NOTES:
// - Presenter (ADR-0001): no markup
// - Publishing and stopping ask for step-up; the component retries after it
// =========================================================
public sealed class MaintenancePresenter : PlatformPresenter<MaintenanceState>
{
    private readonly ListPlatformMaintenanceWindowsUseCase _list;

    private readonly SchedulePlatformMaintenanceWindowUseCase _schedule;

    private readonly StopPlatformMaintenanceWindowUseCase _stop;

    public MaintenancePresenter(
        ListPlatformMaintenanceWindowsUseCase list,
        SchedulePlatformMaintenanceWindowUseCase schedule,
        StopPlatformMaintenanceWindowUseCase stop,
        PlatformUseCaseScope scope,
        ILogger<MaintenancePresenter> logger)
        : base(scope, MaintenanceState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        _stop = stop ?? throw new ArgumentNullException(nameof(stop));
    }

    public async Task LoadAsync(int page)
    {
        ListPlatformMaintenanceWindowsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformMaintenanceWindowsRequest { Page = Math.Max(page, 1) }, cancellationToken);
            return response.Error;
        });

        SetState(error is null && response?.Past is { } past
            ? State with { Loaded = true, Open = response.Open, Past = past.Items, Page = past.Page, TotalPages = past.TotalPages, CanManage = response.CanManage }
            : State with { Loaded = true, Error = error });
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    public async Task<bool> ScheduleAsync(MaintenanceFormModel model, int page)
    {
        ArgumentNullException.ThrowIfNull(model);

        SchedulePlatformMaintenanceWindowResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _schedule.ExecuteAsync(context, new SchedulePlatformMaintenanceWindowRequest
            {
                StartsUtc = new DateTimeOffset(DateTime.SpecifyKind(model.StartsUtc, DateTimeKind.Utc)),
                EndsUtc = new DateTimeOffset(DateTime.SpecifyKind(model.EndsUtc, DateTimeKind.Utc)),
                Sites = model.Sites(),
                NoticeHours = model.NoticeHours,
                MessageEn = model.MessageEn,
                MessageFr = model.MessageFr,
                MessageEs = model.MessageEs
            }, cancellationToken);
            return response.Error;
        });

        return await AfterAsync(error, "ui.maintenance.scheduled", error is null ? response?.PassToken : null, page);
    }

    public async Task<bool> StopAsync(Guid windowId, int page)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
            (await _stop.ExecuteAsync(context, new StopPlatformMaintenanceWindowRequest { WindowId = windowId }, cancellationToken)).Error);

        return await AfterAsync(error, "ui.maintenance.stopped", null, page);
    }

    private async Task<bool> AfterAsync(Error? error, string doneKey, string? passToken, int page)
    {
        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null, DoneKey = null });
            return false;
        }

        SetState(State with { StepUpPending = false, DoneKey = error is null ? doneKey : null, PassToken = passToken ?? State.PassToken, Error = error });

        if (error is null)
        {
            await LoadAsync(page);
        }

        return error is null;
    }
}
