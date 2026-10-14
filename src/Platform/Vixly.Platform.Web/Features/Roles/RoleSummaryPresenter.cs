using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Roles;
using Vixly.Platform.Application.UseCases.Roles.DeactivatePlatformRole;
using Vixly.Platform.Application.UseCases.Roles.GetPlatformRole;
using Vixly.Platform.Application.UseCases.Roles.ReactivatePlatformRole;
using Vixly.Platform.Application.UseCases.Roles.UpdatePlatformRole;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Roles;

// =========================================================
// FILE: RoleSummaryPresenter.cs
//
// PURPOSE:
// Behaviour of the role summary: load, rename, deactivate and
// reactivate.
//
// RESPONSIBILITIES:
// - Load the role and the actions the user may take
// - Rename, deactivate (after a confirmation) and reactivate
// - Ask for step-up when reactivation needs it, then resume
//
// IMPORTANT NOTES:
// - Plain C# (UI-4): no Blazor types
// - Every call runs in a fresh use-case scope (PlatformUseCaseScope)
// =========================================================
public sealed class RoleSummaryPresenter : PlatformPresenter<RoleSummaryState>
{
    private readonly GetPlatformRoleUseCase _get;

    private readonly UpdatePlatformRoleUseCase _rename;

    private readonly DeactivatePlatformRoleUseCase _deactivate;

    private readonly ReactivatePlatformRoleUseCase _reactivate;

    public RoleSummaryPresenter(
        GetPlatformRoleUseCase get,
        UpdatePlatformRoleUseCase rename,
        DeactivatePlatformRoleUseCase deactivate,
        ReactivatePlatformRoleUseCase reactivate,
        PlatformUseCaseScope scope,
        ILogger<RoleSummaryPresenter> logger)
        : base(scope, RoleSummaryState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _rename = rename ?? throw new ArgumentNullException(nameof(rename));
        _deactivate = deactivate ?? throw new ArgumentNullException(nameof(deactivate));
        _reactivate = reactivate ?? throw new ArgumentNullException(nameof(reactivate));
    }

    public async Task LoadAsync(Guid platformRoleId)
    {
        GetPlatformRoleResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformRoleRequest { PlatformRoleId = platformRoleId }, cancellationToken);

            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Role = response.Role, Actions = response.Actions }
            : State with { Role = null, Actions = PlatformRoleActions.None, Error = error });
    }

    public void StartRename()
    {
        SetState(State with { Renaming = true, ConfirmingDeactivate = false, DoneKey = null, Error = null });
    }

    public void StartDeactivate()
    {
        SetState(State with { ConfirmingDeactivate = true, Renaming = false, DoneKey = null, Error = null });
    }

    public void Cancel()
    {
        SetState(State with { Renaming = false, ConfirmingDeactivate = false, StepUpPending = false, Error = null });
    }

    public Task RenameAsync(Guid platformRoleId, string name)
    {
        return RunAndReloadAsync(platformRoleId, "ui.roles.renamed", async (context, cancellationToken) =>
            (await _rename.ExecuteAsync(context, new UpdatePlatformRoleRequest { PlatformRoleId = platformRoleId, Name = name ?? string.Empty }, cancellationToken)).Error);
    }

    public Task DeactivateAsync(Guid platformRoleId)
    {
        return RunAndReloadAsync(platformRoleId, "ui.roles.deactivated", async (context, cancellationToken) =>
            (await _deactivate.ExecuteAsync(context, new DeactivatePlatformRoleRequest { PlatformRoleId = platformRoleId }, cancellationToken)).Error);
    }

    public Task ReactivateAsync(Guid platformRoleId)
    {
        return RunAndReloadAsync(platformRoleId, "ui.roles.reactivated", async (context, cancellationToken) =>
            (await _reactivate.ExecuteAsync(context, new ReactivatePlatformRoleRequest { PlatformRoleId = platformRoleId }, cancellationToken)).Error);
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false });
    }

    private async Task RunAndReloadAsync(
        Guid platformRoleId,
        string doneKey,
        Func<PlatformAuthorizationContext, CancellationToken, Task<Error?>> work)
    {
        Error? error = await RunUseCaseAsync(work);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Error = null });
            return;
        }

        if (error is not null)
        {
            SetState(State with { Error = error });
            return;
        }

        SetState(State with { Renaming = false, ConfirmingDeactivate = false, StepUpPending = false, DoneKey = doneKey, Error = null });
        await LoadAsync(platformRoleId);
    }
}
