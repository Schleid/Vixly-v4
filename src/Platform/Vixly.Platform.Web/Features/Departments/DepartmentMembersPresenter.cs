using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Departments.GetPlatformDepartment;
using Vixly.Platform.Application.UseCases.Departments.RemovePlatformDepartmentMember;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Departments;

// =========================================================
// FILE: DepartmentMembersPresenter.cs
//
// PURPOSE:
// Behaviour of the members list of a department.
//
// RESPONSIBILITIES:
// - Load the members
// - Take a member out of the department
//
// IMPORTANT NOTES:
// - Plain C# (UI-4): no Blazor types
// - Every call runs in a fresh use-case scope (PlatformUseCaseScope)
// - Members are added from the user page
// =========================================================
public sealed class DepartmentMembersPresenter : PlatformPresenter<DepartmentMembersState>
{
    private readonly GetPlatformDepartmentUseCase _get;

    private readonly RemovePlatformDepartmentMemberUseCase _remove;

    public DepartmentMembersPresenter(
        GetPlatformDepartmentUseCase get,
        RemovePlatformDepartmentMemberUseCase remove,
        PlatformUseCaseScope scope,
        ILogger<DepartmentMembersPresenter> logger)
        : base(scope, DepartmentMembersState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
        _remove = remove ?? throw new ArgumentNullException(nameof(remove));
    }

    public async Task LoadAsync(Guid platformDepartmentId)
    {
        GetPlatformDepartmentResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformDepartmentRequest { PlatformDepartmentId = platformDepartmentId }, cancellationToken);

            return response.Error;
        });

        SetState(error is null && response?.Department is { } department
            ? State with { Loaded = true, Members = department.Members, CanManage = response.Actions.CanManageMembers }
            : State with { Loaded = true, CanManage = false, Error = error });
    }

    public async Task RemoveAsync(Guid platformDepartmentId, Guid platformUserId)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
            (await _remove.ExecuteAsync(
                context,
                new RemovePlatformDepartmentMemberRequest { PlatformUserId = platformUserId, PlatformDepartmentId = platformDepartmentId },
                cancellationToken)).Error);

        SetState(State with { Removed = error is null, Error = error });

        if (error is null)
        {
            await LoadAsync(platformDepartmentId);
        }
    }
}
