using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Roles.GetPlatformRole;
using Vixly.Platform.Application.UseCases.Roles.UpdateRolePermissions;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Roles;

// =========================================================
// FILE: RolePermissionsPresenter.cs
//
// PURPOSE:
// Behaviour of the permission editor of one role.
//
// RESPONSIBILITIES:
// - Load the role's permissions and whether the user may edit them
// - Save the ticked permissions; ask for step-up when needed and
//   resume with the same selection
//
// IMPORTANT NOTES:
// - Plain C# (UI-4): no Blazor types
// - Every call runs in a fresh use-case scope (PlatformUseCaseScope)
// =========================================================
public sealed class RolePermissionsPresenter : PlatformPresenter<RolePermissionsState>
{
    private readonly GetPlatformRoleUseCase _get;

    private readonly UpdateRolePermissionsUseCase _update;

    public RolePermissionsPresenter(
        GetPlatformRoleUseCase get,
        UpdateRolePermissionsUseCase update,
        PlatformUseCaseScope scope,
        ILogger<RolePermissionsPresenter> logger)
        : base(scope, RolePermissionsState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _update = update ?? throw new ArgumentNullException(nameof(update));
    }

    public static IReadOnlyList<IGrouping<string, string>> Areas { get; } = PlatformPermissions.Grantable
        .GroupBy(code => code.Split('.')[1])
        .ToList();

    public async Task LoadAsync(Guid platformRoleId)
    {
        GetPlatformRoleResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformRoleRequest { PlatformRoleId = platformRoleId }, cancellationToken);

            return response.Error;
        });

        SetState(error is null && response?.Role is { } role
            ? State with
            {
                Loaded = true,
                IsSystem = role.SystemKey is not null,
                Saved = role.PermissionCodes.ToHashSet(StringComparer.Ordinal),
                CanEdit = response.Actions.CanEditPermissions
            }
            : State with { Loaded = true, CanEdit = false, Error = error });
    }

    public async Task SaveAsync(Guid platformRoleId, IReadOnlyCollection<string> permissionCodes)
    {
        ArgumentNullException.ThrowIfNull(permissionCodes);

        List<string> codes = permissionCodes.ToList();
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
            (await _update.ExecuteAsync(
                context,
                new UpdateRolePermissionsRequest { PlatformRoleId = platformRoleId, PermissionCodes = codes },
                cancellationToken)).Error);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { StepUpPending = true, Pending = codes, Done = false, Error = null });
            return;
        }

        SetState(State with { StepUpPending = false, Pending = Array.Empty<string>(), Done = error is null, Error = error });

        if (error is null)
        {
            await LoadAsync(platformRoleId);
        }
    }

    public Task ResumeAfterStepUpAsync(Guid platformRoleId)
    {
        IReadOnlyList<string> pending = State.Pending;
        SetState(State with { StepUpPending = false });

        return SaveAsync(platformRoleId, pending);
    }

    public void CancelStepUp()
    {
        SetState(State with { StepUpPending = false, Pending = Array.Empty<string>() });
    }
}
