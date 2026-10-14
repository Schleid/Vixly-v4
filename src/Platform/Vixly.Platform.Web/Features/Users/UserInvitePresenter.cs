using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.MyAccount.GetMyPlatformAccess;
using Vixly.Platform.Application.UseCases.Users.InvitePlatformUser;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Users;

// =========================================================
// FILE: UserInvitePresenter.cs
//
// PURPOSE:
// Behaviour of the invite form.
//
// RESPONSIBILITIES:
// - Decide whether the form and the authority choice show
// - Send the invitation and report the new user, or the error
//
// IMPORTANT NOTES:
// - Nobody sets a password for someone else; the invitation
//   email carries a link (D-S1)
// =========================================================
public sealed class UserInvitePresenter : PlatformPresenter<UserInviteState>
{
    private readonly InvitePlatformUserUseCase _invite;

    private readonly GetMyPlatformAccessUseCase _access;

    public UserInvitePresenter(
        InvitePlatformUserUseCase invite,
        GetMyPlatformAccessUseCase access,
        PlatformUseCaseScope scope,
        ILogger<UserInvitePresenter> logger)
        : base(scope, UserInviteState.Initial, logger)
    {
        _invite = invite ?? throw new ArgumentNullException(nameof(invite));
        _access = access ?? throw new ArgumentNullException(nameof(access));
    }

    public async Task LoadAsync()
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            GetMyPlatformAccessResponse access = await _access.ExecuteAsync(context, new GetMyPlatformAccessRequest(), cancellationToken);
            SetState(State with
            {
                Loaded = true,
                CanInvite = access.PermissionCodes.Contains(PlatformPermissions.UsersCreate),
                CanChooseAuthority = access.PermissionCodes.Contains(PlatformPermissions.UsersChangeAuthority)
            });

            return null;
        });

        if (error is not null)
        {
            SetState(State with { Loaded = true, Error = error });
        }
    }

    public async Task InviteAsync(UserInviteInput input)
    {
        ArgumentNullException.ThrowIfNull(input);

        Guid? invited = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            InvitePlatformUserResponse response = await _invite.ExecuteAsync(
                context,
                new InvitePlatformUserRequest
                {
                    FirstName = input.FirstName,
                    LastName = input.LastName,
                    Email = input.Email,
                    PreferredLanguage = input.PreferredLanguage,
                    AuthorityLevel = input.AuthorityLevel,
                    EmployeeNumber = string.IsNullOrWhiteSpace(input.EmployeeNumber) ? null : input.EmployeeNumber,
                    PhoneNumber = input.PhoneNumber
                },
                cancellationToken);
            invited = response.Succeeded ? response.PlatformUserId : null;

            return response.Error;
        });

        SetState(State with { InvitedUserId = error is null ? invited : null, Error = error });
    }
}
