using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.GetPlatformTenantLicensing;

// =========================================================
// FILE: GetPlatformTenantLicensingUseCase.cs
//
// PURPOSE:
// Reads the Licensing section of a tenant page: seats, devices by state and actions (D-L15).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformTenantLicensingUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public GetPlatformTenantLicensingUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<GetPlatformTenantLicensingResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformTenantLicensingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _licensing.AuthorizeAsync(context, PlatformPermissions.LicensingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformTenantLicensingResponse.Failure(refused);
        }

        PlatformSubscription? subscription = await _licensing.Subscriptions.Subscriptions.GetCurrentAsync(request.PlatformTenantId, cancellationToken);
        int bought = subscription?.Status == PlatformSubscriptionStatus.Ended ? 0 : subscription?.Licences ?? 0;
        PlatformLicensingCounts counts = await _licensing.Queries.CountAsync(request.PlatformTenantId, _licensing.UtcNow, cancellationToken);
        PlatformTenant? tenant = await _licensing.Tenants.GetAsync(request.PlatformTenantId, cancellationToken);
        PlatformTenantRegistration? registration = await _licensing.Registrations.FindByTenantAsync(request.PlatformTenantId, cancellationToken);
        bool firstDevice = counts.HoldingSeats == 0
            && (tenant?.Status == PlatformTenantStatus.Active
                || registration?.Status is PlatformTenantRegistrationStatus.PendingAccountSetup or PlatformTenantRegistrationStatus.PendingFirstDeviceRegistration);

        return GetPlatformTenantLicensingResponse.Success(new PlatformTenantLicensingSummary(
            bought,
            subscription?.ScheduledChange is { } change && change.TargetLicences != bought ? change.TargetLicences : null,
            counts,
            PlatformLicensingLimits.AllowanceFor(bought),
            firstDevice,
            new PlatformLicensingAbilities(
                await _licensing.HoldsAsync(context, PlatformPermissions.LicensingManage, cancellationToken),
                await _licensing.HoldsAsync(context, PlatformPermissions.LicensingApproveRelease, cancellationToken))));
    }
}
