using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.Tenants.UpdatePlatformTenantSettings;

// =========================================================
// FILE: UpdatePlatformTenantSettingsUseCase.cs
//
// PURPOSE:
// Changes a tenant's default language and time zone.
//
// RESPONSIBILITIES:
// - Check platform.tenants.edit
// - Validate the language (en, fr, es) and the IANA time zone
// - Audit before and after
//
// IMPORTANT NOTES:
// - Notices are sent in the default language
// =========================================================
public sealed class UpdatePlatformTenantSettingsUseCase
{
    private readonly PlatformTenantAdministration _administration;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public UpdatePlatformTenantSettingsUseCase(
        PlatformTenantAdministration administration,
        IPlatformUnitOfWork unitOfWork)
    {
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<UpdatePlatformTenantSettingsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformTenantSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        PlatformTenantTarget target = await _administration.LoadTenantAsync(
            context,
            PlatformPermissions.TenantsEdit,
            request.PlatformTenantId,
            requireStepUp: false,
            cancellationToken);

        if (target.Tenant is not PlatformTenant tenant)
        {
            return UpdatePlatformTenantSettingsResponse.Failure(target.Error!);
        }

        object before = new { language = tenant.DefaultLanguage.Value, timeZone = tenant.TimeZone };

        try
        {
            tenant.UpdateSettings(LanguageCode.Create(request.Language), request.TimeZone, _administration.UtcNow);
        }
        catch (DomainException exception)
        {
            return UpdatePlatformTenantSettingsResponse.Failure(exception.Error);
        }

        _administration.RecordChange(context, tenant, PlatformPermissions.TenantsEdit, "platform.tenants.settings_updated", before, new { language = tenant.DefaultLanguage.Value, timeZone = tenant.TimeZone });

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return UpdatePlatformTenantSettingsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
        }

        return UpdatePlatformTenantSettingsResponse.Success();
    }
}
