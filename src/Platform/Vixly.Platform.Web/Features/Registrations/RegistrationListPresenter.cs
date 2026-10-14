using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Registrations.ListPlatformTenantRegistrations;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Registrations;

// =========================================================
// FILE: RegistrationListPresenter.cs
//
// PURPOSE:
// Behaviour of the registrations list.
//
// RESPONSIBILITIES:
// - Load one filtered page of registrations
//
// IMPORTANT NOTES:
// - Filters and page come from the URL
// =========================================================
public sealed class RegistrationListPresenter : PlatformPresenter<RegistrationListState>
{
    private readonly ListPlatformTenantRegistrationsUseCase _list;

    public RegistrationListPresenter(ListPlatformTenantRegistrationsUseCase list, PlatformUseCaseScope scope, ILogger<RegistrationListPresenter> logger)
        : base(scope, RegistrationListState.Initial, logger)
    {
        _list = list ?? throw new ArgumentNullException(nameof(list));
    }

    public async Task LoadAsync(string? search, PlatformTenantRegistrationStatus? status, int page)
    {
        ListPlatformTenantRegistrationsResponse? response = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            response = await _list.ExecuteAsync(
                context,
                new ListPlatformTenantRegistrationsRequest { Search = search, Status = status, Page = page },
                cancellationToken);

            return response.Error;
        });

        SetState(error is null && response?.Registrations is { } registrations
            ? new RegistrationListState(registrations.Items, registrations.Page, registrations.TotalPages, registrations.TotalCount, true, null)
            : RegistrationListState.Initial with { Loaded = true, Error = error });
    }
}
