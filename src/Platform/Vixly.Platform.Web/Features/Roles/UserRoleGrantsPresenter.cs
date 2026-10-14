using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Roles.AssignPlatformRole;
using Vixly.Platform.Application.UseCases.Roles.GetPlatformUserRoles;
using Vixly.Platform.Application.UseCases.Roles.RevokePlatformRole;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Roles;

// =========================================================
// FILE: UserRoleGrantsPresenter.cs
//
// PURPOSE:
// Behaviour of the roles section of a user: give and remove
// roles.
//
// RESPONSIBILITIES:
// - Load the grants and the roles the signed-in user may give
// - Give a role, for good or until the end of a chosen day (UTC)
// - Remove a role
// - Ask for step-up when needed, then resume the same change
//
// IMPORTANT NOTES:
// - Plain C# (UI-4): no Blazor types
// - Every call runs in a fresh use-case scope (PlatformUseCaseScope)
// - A chosen last day means the grant ends at 23:59:59 UTC that
//   day
// =========================================================
public sealed class UserRoleGrantsPresenter : PlatformPresenter<UserRoleGrantsState>
{
    private readonly GetPlatformUserRolesUseCase _get;

    private readonly AssignPlatformRoleUseCase _assign;

    private readonly RevokePlatformRoleUseCase _revoke;

    public UserRoleGrantsPresenter(
        GetPlatformUserRolesUseCase get,
        AssignPlatformRoleUseCase assign,
        RevokePlatformRoleUseCase revoke,
        PlatformUseCaseScope scope,
        ILogger<UserRoleGrantsPresenter> logger)
        : base(scope, UserRoleGrantsState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _assign = assign ?? throw new ArgumentNullException(nameof(assign));
        _revoke = revoke ?? throw new ArgumentNullException(nameof(revoke));
    }

    public static DateTimeOffset? EndOf(DateOnly? lastDay)
    {
        return lastDay is DateOnly day
            ? new DateTimeOffset(day.ToDateTime(new TimeOnly(23, 59, 59)), TimeSpan.Zero)
            : null;
    }

    public async Task LoadAsync(Guid platformUserId)
    {
        GetPlatformUserRolesResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformUserRolesRequest { PlatformUserId = platformUserId }, cancellationToken);

            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Loaded = true, Grants = response.Grants, Assignable = response.AssignableRoles, CanManage = response.CanManage }
            : State with { Loaded = true, CanManage = false, Error = error });
    }

    public Task<bool> AssignAsync(Guid platformUserId, Guid platformRoleId, DateOnly? lastDay)
    {
        return ApplyAsync(platformUserId, new PendingRoleChange(true, platformRoleId, EndOf(lastDay)));
    }

    public Task<bool> RevokeAsync(Guid platformUserId, Guid platformRoleId)
    {
        return ApplyAsync(platformUserId, new PendingRoleChange(false, platformRoleId, null));
    }

    public Task<bool> ResumeAfterStepUpAsync(Guid platformUserId)
    {
        PendingRoleChange? pending = State.Pending;
        SetState(State with { Pending = null });

        return pending is null ? Task.FromResult(false) : ApplyAsync(platformUserId, pending);
    }

    public void CancelStepUp()
    {
        SetState(State with { Pending = null });
    }

    private async Task<bool> ApplyAsync(Guid platformUserId, PendingRoleChange change)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => change.IsAssign
            ? (await _assign.ExecuteAsync(
                context,
                new AssignPlatformRoleRequest { PlatformUserId = platformUserId, PlatformRoleId = change.PlatformRoleId, ExpiresUtc = change.ExpiresUtc },
                cancellationToken)).Error
            : (await _revoke.ExecuteAsync(
                context,
                new RevokePlatformRoleRequest { PlatformUserId = platformUserId, PlatformRoleId = change.PlatformRoleId },
                cancellationToken)).Error);

        if (PlatformCommonErrors.StepUpRequired.Equals(error))
        {
            SetState(State with { Pending = change, DoneKey = null, Error = null });
            return false;
        }

        SetState(State with { Pending = null, DoneKey = error is null ? (change.IsAssign ? "ui.user_roles.assigned" : "ui.user_roles.revoked") : null, Error = error });

        if (error is null)
        {
            await LoadAsync(platformUserId);
        }

        return error is null;
    }
}
