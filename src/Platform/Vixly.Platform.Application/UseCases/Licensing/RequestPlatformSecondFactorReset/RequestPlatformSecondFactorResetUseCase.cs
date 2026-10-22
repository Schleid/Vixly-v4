using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.RequestPlatformSecondFactorReset;

// =========================================================
// FILE: RequestPlatformSecondFactorResetUseCase.cs
//
// PURPOSE:
// Vixly staff ask to reset the Owner's second factor; a second staff member must approve (D-O13).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - platform.licensing.manage with step-up
// - Nothing is emailed until a second staff member approves
// =========================================================
public sealed class RequestPlatformSecondFactorResetUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public RequestPlatformSecondFactorResetUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<RequestPlatformSecondFactorResetResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RequestPlatformSecondFactorResetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _licensing.AuthorizeAsync(context, PlatformPermissions.LicensingManage, requireStepUp: true, cancellationToken) is { } refused)
        {
            return RequestPlatformSecondFactorResetResponse.Failure(refused);
        }

        PlatformTenant? tenant = await _licensing.Tenants.GetAsync(request.PlatformTenantId, cancellationToken);

        if (tenant is null || tenant.Status != PlatformTenantStatus.Active)
        {
            return RequestPlatformSecondFactorResetResponse.Failure(PlatformLicensingErrors.TenantNotActive);
        }

        PlatformOwnerSupportRequest support;

        try
        {
            support = PlatformOwnerSupportRequest.RequestSecondFactorReset(tenant.Id, request.Reason, request.IdentityCheck, context.PlatformUserId, _licensing.UtcNow);
        }
        catch (DomainException exception)
        {
            return RequestPlatformSecondFactorResetResponse.Failure(exception.Error);
        }

        _licensing.Store.AddSupportRequest(support);
        _licensing.Audit(context.PlatformUserId, "platform.licensing.second_factor_reset_requested", PlatformPermissions.LicensingManage, nameof(PlatformTenant), tenant.Id,
            new { request = support.Id, identityCheck = support.IdentityCheck }, support.Reason);

        return await _licensing.SaveAsync(cancellationToken)
            ? RequestPlatformSecondFactorResetResponse.Success(support.Id)
            : RequestPlatformSecondFactorResetResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
