using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Roles.ListPlatformRoles;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Roles;

// =========================================================
// FILE: RoleListPresenter.cs
//
// PURPOSE:
// Behaviour of the roles list.
//
// RESPONSIBILITIES:
// - Load the roles, with or without inactive ones
//
// IMPORTANT NOTES:
// - Plain C# (UI-4): no Blazor types
// - Every call runs in a fresh use-case scope (PlatformUseCaseScope)
// =========================================================
public sealed class RoleListPresenter : PlatformPresenter<RoleListState>
{
    private readonly ListPlatformRolesUseCase _list;

    public RoleListPresenter(
        ListPlatformRolesUseCase list,
        PlatformUseCaseScope scope,
        ILogger<RoleListPresenter> logger)
        : base(scope, RoleListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(bool includeInactive)
    {
        ListPlatformRolesResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformRolesRequest { IncludeInactive = includeInactive }, cancellationToken);

            return response.Error;
        });

        SetState(error is null && response is not null
            ? new RoleListState(true, response.Roles, response.CanCreate, null)
            : RoleListState.Initial with { Loaded = true, Error = error });
    }
}
