using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Roles.ListPermissionHolders;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Roles;

// =========================================================
// FILE: PermissionHoldersPresenter.cs
//
// PURPOSE:
// Behaviour of the "who has this permission" list.
//
// RESPONSIBILITIES:
// - Load the holders of one permission code
//
// IMPORTANT NOTES:
// - Plain C# (UI-4): no Blazor types
// - Every call runs in a fresh use-case scope (PlatformUseCaseScope)
// =========================================================
public sealed class PermissionHoldersPresenter : PlatformPresenter<PermissionHoldersState>
{
    private readonly ListPermissionHoldersUseCase _list;

    public PermissionHoldersPresenter(
        ListPermissionHoldersUseCase list,
        PlatformUseCaseScope scope,
        ILogger<PermissionHoldersPresenter> logger)
        : base(scope, PermissionHoldersState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(string permissionCode)
    {
        ListPermissionHoldersResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(
                context,
                new ListPermissionHoldersRequest { PermissionCode = permissionCode ?? string.Empty },
                cancellationToken);

            return response.Error;
        });

        SetState(error is null && response is not null
            ? new PermissionHoldersState(true, response.Holders, null)
            : PermissionHoldersState.Initial with { Loaded = true, Error = error });
    }
}
