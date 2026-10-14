using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.UseCases.Tenants.CreatePlatformTenant;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantCreatePresenter.cs
//
// PURPOSE:
// Behaviour of the new-tenant form (D-T8).
//
// RESPONSIBILITIES:
// - Create the tenant in Draft and report its id
//
// IMPORTANT NOTES:
// - The form explains that the address is fixed once the tenant
//   goes live (D-T11)
// =========================================================
public sealed class TenantCreatePresenter : PlatformPresenter<TenantCreateState>
{
    private readonly CreatePlatformTenantUseCase _create;

    public TenantCreatePresenter(
        CreatePlatformTenantUseCase create,
        IPlatformTenantPolicy policy,
        PlatformUseCaseScope scope,
        ILogger<TenantCreatePresenter> logger)
        : base(scope, TenantCreateState.Initial with { BaseDomain = policy?.TenantBaseDomain ?? string.Empty }, logger)
    {
        _create = create ?? throw new ArgumentNullException(nameof(create));
        ArgumentNullException.ThrowIfNull(policy);
    }

    public async Task CreateAsync(TenantFormModel form)
    {
        ArgumentNullException.ThrowIfNull(form);

        Guid? created = null;
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            CreatePlatformTenantResponse response = await _create.ExecuteAsync(
                context,
                new CreatePlatformTenantRequest
                {
                    Company = form.ToCompany(),
                    Slug = form.Slug,
                    OwnerName = form.OwnerName,
                    OwnerEmail = form.OwnerEmail,
                    OwnerPhone = form.OwnerPhone,
                    Language = form.Language,
                    TimeZone = form.TimeZone
                },
                cancellationToken);
            created = response.Succeeded ? response.PlatformTenantId : null;

            return response.Error;
        });

        SetState(State with { CreatedTenantId = error is null ? created : null, Error = error });
    }
}
