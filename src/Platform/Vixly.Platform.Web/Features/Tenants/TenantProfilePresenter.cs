using Microsoft.Extensions.Logging;
using Vixly.Platform.Application.UseCases.Tenants.UpdatePlatformTenantProfile;
using Vixly.Platform.Application.UseCases.Tenants.UpdatePlatformTenantSettings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Web.Features.Tenants;

// =========================================================
// FILE: TenantProfilePresenter.cs
//
// PURPOSE:
// Behaviour of the company profile section.
//
// RESPONSIBILITIES:
// - Save the company details, then the language and time zone
//
// IMPORTANT NOTES:
// - Two use cases, each audited; the second runs only if the first
//   succeeded
// =========================================================
public sealed class TenantProfilePresenter : PlatformPresenter<TenantProfileState>
{
    private readonly UpdatePlatformTenantProfileUseCase _profile;

    private readonly UpdatePlatformTenantSettingsUseCase _settings;

    public TenantProfilePresenter(
        UpdatePlatformTenantProfileUseCase profile,
        UpdatePlatformTenantSettingsUseCase settings,
        PlatformUseCaseScope scope,
        ILogger<TenantProfilePresenter> logger)
        : base(scope, TenantProfileState.Initial, logger)
    {
        _profile = profile ?? throw new ArgumentNullException(nameof(profile));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public void StartEdit()
    {
        SetState(new TenantProfileState(true, null, null));
    }

    public void Cancel()
    {
        SetState(TenantProfileState.Initial);
    }

    public async Task<bool> SaveAsync(Guid platformTenantId, TenantFormModel form)
    {
        ArgumentNullException.ThrowIfNull(form);

        Error? error = await RunUseCaseAsync(async (context, cancellationToken) =>
        {
            Error? failed = (await _profile.ExecuteAsync(
                context,
                new UpdatePlatformTenantProfileRequest { PlatformTenantId = platformTenantId, Company = form.ToCompany() },
                cancellationToken)).Error;

            return failed ?? (await _settings.ExecuteAsync(
                context,
                new UpdatePlatformTenantSettingsRequest { PlatformTenantId = platformTenantId, Language = form.Language, TimeZone = form.TimeZone },
                cancellationToken)).Error;
        });

        SetState(error is null ? new TenantProfileState(false, "ui.tenants.profile_saved", null) : State with { Error = error });

        return error is null;
    }
}
