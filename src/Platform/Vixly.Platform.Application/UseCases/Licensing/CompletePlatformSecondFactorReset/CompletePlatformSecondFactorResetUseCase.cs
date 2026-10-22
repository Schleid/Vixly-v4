using Vixly.Platform.Application.Licensing;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Licensing;
using Vixly.Platform.Domain.Licensing;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Licensing.CompletePlatformSecondFactorReset;

// =========================================================
// FILE: CompletePlatformSecondFactorResetUseCase.cs
//
// PURPOSE:
// Uses an emailed second-factor reset link on the tenant host (D-O13).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The link proves the Owner (it went to the Owner contact only); Operations also checks the password
// - A link of another tenant, a used, expired or voided link: one answer, link_invalid
// =========================================================
public sealed class CompletePlatformSecondFactorResetUseCase
{
    private readonly PlatformLicensingAdministration _licensing;

    public CompletePlatformSecondFactorResetUseCase(
        PlatformLicensingAdministration licensing)
    {
        _licensing = licensing ?? throw new ArgumentNullException(nameof(licensing));
    }

    public async Task<CompletePlatformSecondFactorResetResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        CompletePlatformSecondFactorResetRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset utcNow = _licensing.UtcNow;
        PlatformDeviceLink? link = string.IsNullOrEmpty(request.Token) ? null : await _licensing.Store.GetLinkAsync(_licensing.Hash(request.Token), cancellationToken);

        if (link is null || link.Kind != PlatformDeviceLinkKind.SecondFactorReset
            || (request.ExpectedTenantId is { } expected && link.PlatformTenantId != expected) || !link.Try(utcNow))
        {
            if (link is not null)
            {
                await _licensing.SaveAsync(cancellationToken);
            }

            return CompletePlatformSecondFactorResetResponse.Failure(PlatformLicensingErrors.LinkInvalid);
        }

        link.MarkUsed(utcNow);

        if (link.SupportRequestId is Guid supportId && await _licensing.Store.GetSupportRequestAsync(supportId, cancellationToken) is { Status: PlatformOwnerSupportStatus.Approved } support)
        {
            support.Complete(utcNow);
        }

        _licensing.Notice(link.PlatformTenantId, PlatformLicensingNoticeKind.SecondFactorResetDone, supportRequestId: link.SupportRequestId, supportKind: PlatformOwnerSupportKind.SecondFactorReset);
        _licensing.Audit(null, "platform.licensing.second_factor_reset_completed", null, nameof(PlatformTenant), link.PlatformTenantId, new { request = link.SupportRequestId });

        return await _licensing.SaveAsync(cancellationToken)
            ? CompletePlatformSecondFactorResetResponse.Success(link.PlatformTenantId)
            : CompletePlatformSecondFactorResetResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
