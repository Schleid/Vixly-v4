using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Users.GetPlatformUser;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Users;

// =========================================================
// FILE: UserSummaryPresenter.cs
//
// PURPOSE:
// Behaviour of the user summary.
//
// RESPONSIBILITIES:
// - Load one staff user's detail
//
// IMPORTANT NOTES:
// - Reloaded when a sibling reports the user changed (UI-3)
// =========================================================
public sealed class UserSummaryPresenter : PlatformPresenter<UserSummaryState>
{
    private readonly GetPlatformUserUseCase _get;

    public UserSummaryPresenter(
        GetPlatformUserUseCase get,
        PlatformUseCaseScope scope,
        ILogger<UserSummaryPresenter> logger)
        : base(scope, UserSummaryState.Initial, logger)
    {
        _get = get ?? throw new ArgumentNullException(nameof(get));
    }

    public async Task LoadAsync(Guid platformUserId)
    {
        GetPlatformUserResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _get.ExecuteAsync(context, new GetPlatformUserRequest { PlatformUserId = platformUserId }, cancellationToken);
            return response.Error;
        });

        SetState(new UserSummaryState(error is null ? response?.User : null, error));
    }
}
