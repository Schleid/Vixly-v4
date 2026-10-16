using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformTenantCards;

// =========================================================
// FILE: GetPlatformTenantCardsUseCase.cs
//
// PURPOSE:
// Reads a tenant's saved cards and automatic-charging consent (D-P3, D-P4).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformTenantCardsUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    public GetPlatformTenantCardsUseCase(
        PlatformPaymentsAdministration payments)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
    }

    public async Task<GetPlatformTenantCardsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformTenantCardsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.BillingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformTenantCardsResponse.Failure(refused);
        }

        PlatformTenantCardProfile? profile = await _payments.Store.GetProfileAsync(request.PlatformTenantId, cancellationToken);
        DateTimeOffset utcNow = _payments.UtcNow;
        PlatformSavedCardItem[] cards = (await _payments.Store.ListCardsAsync(request.PlatformTenantId, cancellationToken))
            .Where(card => card.IsActive)
            .OrderByDescending(card => card.IsDefault)
            .ThenByDescending(card => card.CreatedUtc)
            .Select(card => PlatformPaymentsAdministration.CardItem(card, utcNow))
            .ToArray();

        return GetPlatformTenantCardsResponse.Success(new PlatformTenantCardsItem(
            profile?.AutoCharge ?? false,
            profile?.ConsentEmail,
            profile?.ConsentUtc,
            cards,
            await _payments.ReadyAsync(cancellationToken),
            await _payments.HoldsAsync(context, PlatformPermissions.BillingChargeCards, cancellationToken)));
    }
}
