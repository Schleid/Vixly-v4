using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.MyAccount.GetMyPlatformAccess;
using Vixly.Platform.Application.UseCases.Users.ListPlatformUsers;
using Vixly.Platform.Domain.Identity;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Users;

// =========================================================
// FILE: UserListPresenter.cs
//
// PURPOSE:
// Behaviour of the user list.
//
// RESPONSIBILITIES:
// - Load one page for the search, status and page in the URL
// - Decide whether to offer Invite
//
// IMPORTANT NOTES:
// - Filters live in the URL (UI-3); the component navigates and
//   the presenter reloads
// =========================================================
public sealed class UserListPresenter : PlatformPresenter<UserListState>
{
    private readonly ListPlatformUsersUseCase _list;

    private readonly GetMyPlatformAccessUseCase _access;

    public UserListPresenter(
        ListPlatformUsersUseCase list,
        GetMyPlatformAccessUseCase access,
        PlatformUseCaseScope scope,
        ILogger<UserListPresenter> logger)
        : base(scope, UserListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
        _access = access ?? throw new ArgumentNullException(nameof(access));
    }

    public async Task LoadAsync(string? search, PlatformUserAccountStatus? status, int page)
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            GetMyPlatformAccessResponse access = await _access.ExecuteAsync(context, new GetMyPlatformAccessRequest(), cancellationToken);
            ListPlatformUsersResponse response = await _list.ExecuteAsync(
                context,
                new ListPlatformUsersRequest { Search = search, Status = status, Page = page },
                cancellationToken);

            if (response.Users is null)
            {
                return response.Error;
            }

            SetState(new UserListState(
                response.Users.Items,
                response.Users.Page,
                response.Users.TotalPages,
                response.Users.TotalCount,
                access.PermissionCodes.Contains(PlatformPermissions.UsersCreate),
                true,
                null));

            return null;
        });

        if (error is not null)
        {
            SetState(UserListState.Initial with { Loaded = true, Error = error });
        }
    }
}
