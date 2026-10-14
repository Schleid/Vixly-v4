using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Departments.AddPlatformDepartmentMember;
using Vixly.Platform.Application.UseCases.Departments.GetPlatformUserDepartments;
using Vixly.Platform.Application.UseCases.Departments.RemovePlatformDepartmentMember;
using Vixly.Platform.Application.UseCases.Departments.SetPrimaryPlatformDepartment;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Departments;

// =========================================================
// FILE: UserDepartmentsPresenter.cs
//
// PURPOSE:
// Behaviour of the departments section of a user: add, remove
// and choose the primary department.
//
// RESPONSIBILITIES:
// - Load the memberships and the departments the user can join
// - Add, remove and make primary, then reload
//
// IMPORTANT NOTES:
// - Plain C# (UI-4): no Blazor types
// - Every call runs in a fresh use-case scope (PlatformUseCaseScope)
// =========================================================
public sealed class UserDepartmentsPresenter : PlatformPresenter<UserDepartmentsState>
{
    private readonly GetPlatformUserDepartmentsUseCase _get;

    private readonly AddPlatformDepartmentMemberUseCase _add;

    private readonly RemovePlatformDepartmentMemberUseCase _remove;

    private readonly SetPrimaryPlatformDepartmentUseCase _primary;

    public UserDepartmentsPresenter(
        GetPlatformUserDepartmentsUseCase get,
        AddPlatformDepartmentMemberUseCase add,
        RemovePlatformDepartmentMemberUseCase remove,
        SetPrimaryPlatformDepartmentUseCase primary,
        PlatformUseCaseScope scope,
        ILogger<UserDepartmentsPresenter> logger)
        : base(scope, UserDepartmentsState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _add = add ?? throw new ArgumentNullException(nameof(add));
        _remove = remove ?? throw new ArgumentNullException(nameof(remove));
        _primary = primary ?? throw new ArgumentNullException(nameof(primary));
    }

    public async Task LoadAsync(Guid platformUserId)
    {
        GetPlatformUserDepartmentsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformUserDepartmentsRequest { PlatformUserId = platformUserId }, cancellationToken);

            return response.Error;
        });

        SetState(error is null && response is not null
            ? State with { Loaded = true, Memberships = response.Memberships, Joinable = response.Joinable, CanManage = response.CanManage }
            : State with { Loaded = true, CanManage = false, Error = error });
    }

    public Task<bool> AddAsync(Guid platformUserId, Guid platformDepartmentId)
    {
        return RunAndReloadAsync(platformUserId, "ui.departments.member_added", async (context, cancellationToken) =>
            (await _add.ExecuteAsync(context, new AddPlatformDepartmentMemberRequest { PlatformUserId = platformUserId, PlatformDepartmentId = platformDepartmentId }, cancellationToken)).Error);
    }

    public Task<bool> RemoveAsync(Guid platformUserId, Guid platformDepartmentId)
    {
        return RunAndReloadAsync(platformUserId, "ui.departments.member_removed", async (context, cancellationToken) =>
            (await _remove.ExecuteAsync(context, new RemovePlatformDepartmentMemberRequest { PlatformUserId = platformUserId, PlatformDepartmentId = platformDepartmentId }, cancellationToken)).Error);
    }

    public Task<bool> MakePrimaryAsync(Guid platformUserId, Guid platformDepartmentId)
    {
        return RunAndReloadAsync(platformUserId, "ui.departments.primary_set", async (context, cancellationToken) =>
            (await _primary.ExecuteAsync(context, new SetPrimaryPlatformDepartmentRequest { PlatformUserId = platformUserId, PlatformDepartmentId = platformDepartmentId }, cancellationToken)).Error);
    }

    private async Task<bool> RunAndReloadAsync(
        Guid platformUserId,
        string doneKey,
        Func<PlatformAuthorizationContext, CancellationToken, Task<Error?>> work)
    {
        Error? error = await RunUseCaseAsync(work);
        SetState(State with { DoneKey = error is null ? doneKey : null, Error = error });

        if (error is null)
        {
            await LoadAsync(platformUserId);
        }

        return error is null;
    }
}
