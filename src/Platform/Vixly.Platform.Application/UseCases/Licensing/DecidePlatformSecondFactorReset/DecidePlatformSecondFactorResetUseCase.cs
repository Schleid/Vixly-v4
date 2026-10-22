using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.DecidePlatformSecondFactorReset;

// =========================================================
// FILE: DecidePlatformSecondFactorResetUseCase.cs
//
// PURPOSE:
// A second Vixly staff member approves or refuses a reset of the Owner's second factor (D-O13).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - platform.licensing.manage with step-up; never the staff member who asked
// - Approval emails the link to the Owner contact and tells Owner and Billing
// =========================================================
public sealed class DecidePlatformSecondFactorResetUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public DecidePlatformSecondFactorResetUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<DecidePlatformSecondFactorResetResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        DecidePlatformSecondFactorResetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _licensing.AuthorizeAsync(context, PlatformPermissions.LicensingManage, requireStepUp: true, cancellationToken) is { } refused)
        {
            return DecidePlatformSecondFactorResetResponse.Failure(refused);
        }

        PlatformOwnerSupportRequest? support = await _licensing.Store.GetSupportRequestAsync(request.RequestId, cancellationToken);

        if (support is null || support.Kind != PlatformOwnerSupportKind.SecondFactorReset)
        {
            return DecidePlatformSecondFactorResetResponse.Failure(PlatformCommonErrors.NotFound);
        }

        if (request.Approve && await _licensing.LinkLimitReachedAsync(support.PlatformTenantId, cancellationToken))
        {
            return DecidePlatformSecondFactorResetResponse.Failure(PlatformLicensingErrors.TooManyLinks);
        }

        try
        {
            if (request.Approve)
            {
                support.Approve(context.PlatformUserId, request.Note, _licensing.UtcNow);
            }
            else
            {
                support.Refuse(context.PlatformUserId, request.Note, _licensing.UtcNow);
            }
        }
        catch (DomainException exception)
        {
            return DecidePlatformSecondFactorResetResponse.Failure(exception.Error);
        }

        if (request.Approve)
        {
            _licensing.Store.AddEvents(new[] { PlatformDeviceEvent.Record(support.PlatformTenantId, null, PlatformDeviceEventCodes.LinkSent, PlatformDeviceActor.Staff(context.PlatformUserId), _licensing.UtcNow,
                arguments: new Dictionary<string, string>(StringComparer.Ordinal) { ["kind"] = nameof(PlatformDeviceLinkKind.SecondFactorReset) }) });
            _licensing.Notice(support.PlatformTenantId, PlatformLicensingNoticeKind.SecondFactorResetLink, supportRequestId: support.Id, supportKind: PlatformOwnerSupportKind.SecondFactorReset);
            _licensing.Notice(support.PlatformTenantId, PlatformLicensingNoticeKind.OwnerSupportStarted, supportRequestId: support.Id, supportKind: PlatformOwnerSupportKind.SecondFactorReset);
        }

        _licensing.Audit(context.PlatformUserId, request.Approve ? "platform.licensing.second_factor_reset_approved" : "platform.licensing.second_factor_reset_refused",
            PlatformPermissions.LicensingManage, nameof(PlatformTenant), support.PlatformTenantId, new { request = support.Id, requestedBy = support.RequestedBy }, support.DecisionNote);

        return await _licensing.SaveAsync(cancellationToken)
            ? DecidePlatformSecondFactorResetResponse.Success()
            : DecidePlatformSecondFactorResetResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
