using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Departments.CreatePlatformDepartment;
using Vixly.Platform.Application.UseCases.MyAccount.GetMyPlatformAccess;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Departments;

// =========================================================
// FILE: DepartmentCreatePresenter.cs
//
// PURPOSE:
// Behaviour of the new department form.
//
// RESPONSIBILITIES:
// - Check that the user may create departments
// - Create the department and report its id
//
// IMPORTANT NOTES:
// - Plain C# (UI-4): no Blazor types
// - Every call runs in a fresh use-case scope (PlatformUseCaseScope)
// =========================================================
public sealed class DepartmentCreatePresenter : PlatformPresenter<DepartmentCreateState>
{
    private readonly CreatePlatformDepartmentUseCase _create;

    private readonly GetMyPlatformAccessUseCase _access;

    public DepartmentCreatePresenter(
        CreatePlatformDepartmentUseCase create,
        GetMyPlatformAccessUseCase access,
        PlatformUseCaseScope scope,
        ILogger<DepartmentCreatePresenter> logger)
        : base(scope, DepartmentCreateState.Initial, logger)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        _access = access ?? throw new ArgumentNullException(nameof(access));
    }

    public async Task LoadAsync()
    {
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            GetMyPlatformAccessResponse access = await _access.ExecuteAsync(context, new GetMyPlatformAccessRequest(), cancellationToken);
            SetState(State with { Loaded = true, CanCreate = access.PermissionCodes.Contains(PlatformPermissions.DepartmentsCreate) });

            return null;
        });

        if (error is not null)
        {
            SetState(State with { Loaded = true, Error = error });
        }
    }

    public async Task CreateAsync(string name, string? description)
    {
        Guid? created = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            CreatePlatformDepartmentResponse response = await _create.ExecuteAsync(
                context,
                new CreatePlatformDepartmentRequest { Name = name ?? string.Empty, Description = description },
                cancellationToken);
            created = response.Succeeded ? response.PlatformDepartmentId : null;

            return response.Error;
        });

        SetState(State with { CreatedDepartmentId = error is null ? created : null, Error = error });
    }
}
