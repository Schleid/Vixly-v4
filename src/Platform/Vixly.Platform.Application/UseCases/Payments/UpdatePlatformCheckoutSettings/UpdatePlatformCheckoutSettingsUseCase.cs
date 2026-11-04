using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Payments;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.UpdatePlatformCheckoutSettings;

// =========================================================
// FILE: UpdatePlatformCheckoutSettingsUseCase.cs
//
// PURPOSE:
// Changes the checkout page look (name, logo, icon, colours) and the bank statement wording.
//
// RESPONSIBILITIES:
// - Check the permission and step-up first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// - Same permission as the other payments settings; audited
// =========================================================
public sealed class UpdatePlatformCheckoutSettingsUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    public UpdatePlatformCheckoutSettingsUseCase(PlatformPaymentsAdministration payments)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
    }

    public async Task<UpdatePlatformCheckoutSettingsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        UpdatePlatformCheckoutSettingsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.PaymentsConfigure, requireStepUp: true, cancellationToken) is { } refused)
        {
            return UpdatePlatformCheckoutSettingsResponse.Failure(refused);
        }

        PlatformMerchantAccount account = await _payments.MerchantAsync(cancellationToken);

        try
        {
            account.UpdateCheckout(
                request.DisplayName,
                request.LogoUrl,
                request.IconUrl,
                request.BackgroundColor,
                request.ButtonColor,
                request.StatementPrefix,
                request.StatementSuffix,
                context.PlatformUserId,
                _payments.UtcNow);
        }
        catch (DomainException exception)
        {
            return UpdatePlatformCheckoutSettingsResponse.Failure(exception.Error);
        }

        _payments.Audit(context, "platform.payments.checkout_settings_updated", PlatformPermissions.PaymentsConfigure, nameof(PlatformMerchantAccount), account.Id,
            new
            {
                name = account.CheckoutDisplayName,
                logo = account.CheckoutLogoUrl,
                icon = account.CheckoutIconUrl,
                background = account.CheckoutBackgroundColor,
                button = account.CheckoutButtonColor,
                statement = account.StatementWording ?? account.StatementSuffix
            });

        return await _payments.SaveAsync(cancellationToken)
            ? UpdatePlatformCheckoutSettingsResponse.Success()
            : UpdatePlatformCheckoutSettingsResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
