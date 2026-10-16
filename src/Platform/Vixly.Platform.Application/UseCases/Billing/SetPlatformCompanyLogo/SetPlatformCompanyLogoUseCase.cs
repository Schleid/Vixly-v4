using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.SetPlatformCompanyLogo;

// =========================================================
// FILE: SetPlatformCompanyLogoUseCase.cs
//
// PURPOSE:
// Sets or removes the logo printed on invoices (PNG or JPEG, D-B12).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SetPlatformCompanyLogoUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public SetPlatformCompanyLogoUseCase(
        PlatformBillingAdministration billing,
        IPlatformUnitOfWork unitOfWork)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SetPlatformCompanyLogoResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        SetPlatformCompanyLogoRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingConfigure, requireStepUp: true, cancellationToken) is { } refused)
        {
            return SetPlatformCompanyLogoResponse.Failure(refused);
        }

        PlatformCompanyProfile? profile = await _billing.Store.GetProfileAsync(cancellationToken);

        if (profile is null)
        {
            return SetPlatformCompanyLogoResponse.Failure(PlatformBillingErrors.ProfileMissing);
        }

        try
        {
            profile.SetLogo(request.Content, request.ContentType, context.PlatformUserId, _billing.UtcNow);
        }
        catch (DomainException exception)
        {
            return SetPlatformCompanyLogoResponse.Failure(exception.Error);
        }

        _billing.Audit(context, "platform.billing.company_logo_updated", PlatformPermissions.BillingConfigure, nameof(PlatformCompanyProfile), profile.Id, new { removed = profile.Logo is null });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SetPlatformCompanyLogoResponse.Success()
            : SetPlatformCompanyLogoResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
