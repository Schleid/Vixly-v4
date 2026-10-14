using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Departments.ListPlatformDepartments;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Departments;

// =========================================================
// FILE: DepartmentListPresenter.cs
//
// PURPOSE:
// Behaviour of the departments list.
//
// RESPONSIBILITIES:
// - Load the departments, with or without inactive ones
//
// IMPORTANT NOTES:
// - Plain C# (UI-4): no Blazor types
// - Every call runs in a fresh use-case scope (PlatformUseCaseScope)
// =========================================================
public sealed class DepartmentListPresenter : PlatformPresenter<DepartmentListState>
{
    private readonly ListPlatformDepartmentsUseCase _list;

    public DepartmentListPresenter(
        ListPlatformDepartmentsUseCase list,
        PlatformUseCaseScope scope,
        ILogger<DepartmentListPresenter> logger)
        : base(scope, DepartmentListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(bool includeInactive)
    {
        ListPlatformDepartmentsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(context, new ListPlatformDepartmentsRequest { IncludeInactive = includeInactive }, cancellationToken);

            return response.Error;
        });

        SetState(error is null && response is not null
            ? new DepartmentListState(true, response.Departments, response.CanCreate, null)
            : DepartmentListState.Initial with { Loaded = true, Error = error });
    }
}
