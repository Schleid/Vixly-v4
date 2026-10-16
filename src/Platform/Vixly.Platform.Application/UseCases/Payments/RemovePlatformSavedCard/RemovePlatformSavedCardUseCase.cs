using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.RemovePlatformSavedCard;

// =========================================================
// FILE: RemovePlatformSavedCardUseCase.cs
//
// PURPOSE:
// Removes a saved card and detaches it at the provider (D-P3).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class RemovePlatformSavedCardUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    public RemovePlatformSavedCardUseCase(
        PlatformPaymentsAdministration payments)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
    }

    public async Task<RemovePlatformSavedCardResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        RemovePlatformSavedCardRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.BillingChargeCards, requireStepUp: true, cancellationToken) is { } refused)
        {
            return RemovePlatformSavedCardResponse.Failure(refused);
        }

        PlatformSavedCard? card = await _payments.Store.GetCardAsync(request.SavedCardId, cancellationToken);

        if (card is null || card.PlatformTenantId != request.PlatformTenantId || !card.IsActive)
        {
            return RemovePlatformSavedCardResponse.Failure(PlatformPaymentsErrors.CardNotFound);
        }

        try
        {
            await _payments.Gateway.DetachCardAsync(card.ProviderPaymentMethodId, cancellationToken);
        }
        catch (PlatformCardGatewayException)
        {
            return RemovePlatformSavedCardResponse.Failure(PlatformPaymentsErrors.ProviderUnavailable);
        }

        DateTimeOffset utcNow = _payments.UtcNow;
        bool wasDefault = card.IsDefault;
        card.Remove(context.PlatformUserId, utcNow);

        if (wasDefault)
        {
            (await _payments.Store.ListCardsAsync(request.PlatformTenantId, cancellationToken))
                .Where(other => other.IsActive && other.Id != card.Id)
                .OrderByDescending(other => other.CreatedUtc)
                .FirstOrDefault()?
                .SetDefault(true, utcNow);
        }

        _payments.Audit(context, "platform.payments.card_removed", PlatformPermissions.BillingChargeCards, nameof(PlatformTenant), request.PlatformTenantId, new { brand = card.Brand, last4 = card.Last4 });

        return await _payments.SaveAsync(cancellationToken)
            ? RemovePlatformSavedCardResponse.Success()
            : RemovePlatformSavedCardResponse.Failure(PlatformCommonErrors.ConcurrencyConflict);
    }
}
