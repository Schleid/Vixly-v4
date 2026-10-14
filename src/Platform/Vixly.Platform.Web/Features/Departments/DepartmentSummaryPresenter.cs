using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Departments;
using Vixly.Platform.Application.UseCases.Departments.DeactivatePlatformDepartment;
using Vixly.Platform.Application.UseCases.Departments.GetPlatformDepartment;
using Vixly.Platform.Application.UseCases.Departments.ReactivatePlatformDepartment;
using Vixly.Platform.Application.UseCases.Departments.UpdatePlatformDepartment;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Departments;

// =========================================================
// FILE: DepartmentSummaryPresenter.cs
//
// PURPOSE:
// Behaviour of the department summary: load, edit, deactivate
// and reactivate.
//
// RESPONSIBILITIES:
// - Load the department and the actions the user may take
// - Edit name and description, deactivate (after a confirmation)
//   and reactivate
//
// IMPORTANT NOTES:
// - Plain C# (UI-4): no Blazor types
// - Every call runs in a fresh use-case scope (PlatformUseCaseScope)
// =========================================================
public sealed class DepartmentSummaryPresenter : PlatformPresenter<DepartmentSummaryState>
{
    private readonly GetPlatformDepartmentUseCase _get;

    private readonly UpdatePlatformDepartmentUseCase _update;

    private readonly DeactivatePlatformDepartmentUseCase _deactivate;

    private readonly ReactivatePlatformDepartmentUseCase _reactivate;

    public DepartmentSummaryPresenter(
        GetPlatformDepartmentUseCase get,
        UpdatePlatformDepartmentUseCase update,
        DeactivatePlatformDepartmentUseCase deactivate,
        ReactivatePlatformDepartmentUseCase reactivate,
        PlatformUseCaseScope scope,
        ILogger<DepartmentSummaryPresenter> logger)
        : base(scope, DepartmentSummaryState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _update = update ?? throw new ArgumentNullException(nameof(update));
        _deactivate = deactivate ?? throw new ArgumentNullException(nameof(deactivate));
        _reactivate = reactivate ?? throw new ArgumentNullException(nameof(reactivate));
    }

    public async Task LoadAsync(Guid platformDepartmentId)
    {
        GetPlatformDepartmentResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformDepartmentRequest { PlatformDepartmentId = platformDepartmentId }, cancellationToken);

            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Department = response.Department, Actions = response.Actions }
            : State with { Department = null, Actions = PlatformDepartmentActions.None, Error = error });
    }

    public void StartEdit()
    {
        SetState(State with { Editing = true, ConfirmingDeactivate = false, DoneKey = null, Error = null });
    }

    public void StartDeactivate()
    {
        SetState(State with { ConfirmingDeactivate = true, Editing = false, DoneKey = null, Error = null });
    }

    public void Cancel()
    {
        SetState(State with { Editing = false, ConfirmingDeactivate = false, Error = null });
    }

    public Task UpdateAsync(Guid platformDepartmentId, string name, string? description)
    {
        return RunAndReloadAsync(platformDepartmentId, "ui.departments.updated", async (context, cancellationToken) =>
            (await _update.ExecuteAsync(
                context,
                new UpdatePlatformDepartmentRequest { PlatformDepartmentId = platformDepartmentId, Name = name ?? string.Empty, Description = description },
                cancellationToken)).Error);
    }

    public Task DeactivateAsync(Guid platformDepartmentId)
    {
        return RunAndReloadAsync(platformDepartmentId, "ui.departments.deactivated", async (context, cancellationToken) =>
            (await _deactivate.ExecuteAsync(context, new DeactivatePlatformDepartmentRequest { PlatformDepartmentId = platformDepartmentId }, cancellationToken)).Error);
    }

    public Task ReactivateAsync(Guid platformDepartmentId)
    {
        return RunAndReloadAsync(platformDepartmentId, "ui.departments.reactivated", async (context, cancellationToken) =>
            (await _reactivate.ExecuteAsync(context, new ReactivatePlatformDepartmentRequest { PlatformDepartmentId = platformDepartmentId }, cancellationToken)).Error);
    }

    private async Task RunAndReloadAsync(
        Guid platformDepartmentId,
        string doneKey,
        Func<PlatformAuthorizationContext, CancellationToken, Task<Error?>> work)
    {
        Error? error = await RunUseCaseAsync(work);

        if (error is not null)
        {
            SetState(State with { Error = error });
            return;
        }

        SetState(State with { Editing = false, ConfirmingDeactivate = false, DoneKey = doneKey, Error = null });
        await LoadAsync(platformDepartmentId);
    }
}
