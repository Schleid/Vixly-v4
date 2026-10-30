using Microsoft.Extensions.Logging;
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
// - Save the language and time zone
//
// IMPORTANT NOTES:
// - Legal details change only through an approved change request
//   (B12); this presenter never writes them
// =========================================================
public sealed class TenantProfilePresenter : PlatformPresenter<TenantProfileState>
{
    private readonly UpdatePlatformTenantSettingsUseCase _settings;

    public TenantProfilePresenter(
        UpdatePlatformTenantSettingsUseCase settings,
        PlatformUseCaseScope scope,
        ILogger<TenantProfilePresenter> logger)
        : base(scope, TenantProfileState.Initial, logger)
    {
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

        // B12: legal details are never saved here; only the settings are.
        Error? error = await RunUseCaseAsync(async (context, cancellationToken) => (await _settings.ExecuteAsync(
            context,
            new UpdatePlatformTenantSettingsRequest { PlatformTenantId = platformTenantId, Language = form.Language, TimeZone = form.TimeZone },
            cancellationToken)).Error);

        SetState(error is null ? new TenantProfileState(false, "ui.tenants.profile_saved", null) : State with { Error = error });

        return error is null;
    }
}
