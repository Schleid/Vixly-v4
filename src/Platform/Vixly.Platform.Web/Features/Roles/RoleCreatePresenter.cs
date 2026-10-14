using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Roles.CreatePlatformRole;
using Vixly.Platform.Application.UseCases.MyAccount.GetMyPlatformAccess;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Roles;

// =========================================================
// FILE: RoleCreatePresenter.cs
//
// PURPOSE:
// Behaviour of the new role form.
//
// RESPONSIBILITIES:
// - Check that the user may create roles
// - Create the role and report its id
//
// IMPORTANT NOTES:
// - Plain C# (UI-4): no Blazor types
// - Every call runs in a fresh use-case scope (PlatformUseCaseScope)
// =========================================================
public sealed class RoleCreatePresenter : PlatformPresenter<RoleCreateState>
{
    private readonly CreatePlatformRoleUseCase _create;

    private readonly GetMyPlatformAccessUseCase _access;

    public RoleCreatePresenter(
        CreatePlatformRoleUseCase create,
        GetMyPlatformAccessUseCase access,
        PlatformUseCaseScope scope,
        ILogger<RoleCreatePresenter> logger)
        : base(scope, RoleCreateState.Initial, logger)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _access = access ?? throw new ArgumentNullException(nameof(access));
    }

    public async Task LoadAsync()
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            GetMyPlatformAccessResponse access = await _access.ExecuteAsync(context, new GetMyPlatformAccessRequest(), cancellationToken);
            SetState(State with { Loaded = true, CanCreate = access.PermissionCodes.Contains(PlatformPermissions.RolesCreate) });

            return null;
        });

        if (error is not null)
        {
            SetState(State with { Loaded = true, Error = error });
        }
    }

    public async Task CreateAsync(string name)
    {
        Guid? created = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            CreatePlatformRoleResponse response = await _create.ExecuteAsync(
                context,
                new CreatePlatformRoleRequest { Name = name ?? string.Empty },
                cancellationToken);
            created = response.Succeeded ? response.PlatformRoleId : null;

            return response.Error;
        });

        SetState(State with { CreatedRoleId = error is null ? created : null, Error = error });
    }
}
