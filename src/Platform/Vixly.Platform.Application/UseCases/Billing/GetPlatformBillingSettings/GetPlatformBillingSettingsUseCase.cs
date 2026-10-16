using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.GetPlatformBillingSettings;

// =========================================================
// FILE: GetPlatformBillingSettingsUseCase.cs
//
// PURPOSE:
// Reads billing settings and the company profile (D-B4, D-B12).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformBillingSettingsUseCase
{
    private readonly PlatformBillingAdministration _billing;

    public GetPlatformBillingSettingsUseCase(
        PlatformBillingAdministration billing)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
    }

    public async Task<GetPlatformBillingSettingsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformBillingSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformBillingSettingsResponse.Failure(refused);
        }

        PlatformBillingSettings settings = await _billing.SettingsAsync(cancellationToken);
        PlatformCompanyProfile? profile = await _billing.Store.GetProfileAsync(cancellationToken);

        return GetPlatformBillingSettingsResponse.Success(
            PlatformBillingSettingsMapper.Item(settings, profile),
            await _billing.HoldsAsync(context, PlatformPermissions.BillingConfigure, cancellationToken));
    }
}
