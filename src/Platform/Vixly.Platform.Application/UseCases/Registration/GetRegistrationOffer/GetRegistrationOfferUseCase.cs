using Vixly.Platform.Application.Audit;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases;
using Vixly.Platform.Application.UseCases.Registration;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Application.UseCases.Subscriptions;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;

namespace Vixly.Platform.Application.UseCases.Registration.GetRegistrationOffer;

// =========================================================
// FILE: GetRegistrationOfferUseCase.cs
//
// PURPOSE:
// Shows the terms of an offer link to the prospect (D-S21).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous; Platform.Api rate-limits it like the other registration endpoints
// - Only the token hash is looked up
// =========================================================
public sealed class GetRegistrationOfferUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformPlanRepository _plans;

    public GetRegistrationOfferUseCase(
        PlatformSalesAdministration sales,
        IPlatformPlanRepository plans)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
    }

    public async Task<GetRegistrationOfferResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        GetRegistrationOfferRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset utcNow = _sales.UtcNow;
        PlatformOffer? offer = string.IsNullOrWhiteSpace(request.Token)
            ? null
            : await _sales.Offers.GetByTokenHashAsync(_sales.HashToken(request.Token.Trim()), cancellationToken);
        PlatformPlan? plan = offer is null ? null : await _plans.GetAsync(offer.PlatformPlanId, cancellationToken);

        // Unknown, expired, used, revoked: one answer, so a link cannot be probed.
        if (offer is null || plan is null || offer.Status != PlatformOfferStatus.Issued || offer.IsExpiredAt(utcNow) || !plan.CanBeChosen)
        {
            return GetRegistrationOfferResponse.Failure(PlatformSalesErrors.OfferUnavailable);
        }

        PlatformPlanVersion version = plan.Current!;

        return GetRegistrationOfferResponse.Success(new PlatformRegistrationOfferPreview(
            offer.CompanyName,
            PlatformRegistrationOfferPreview.Mask(offer.BoundEmail),
            plan.Code,
            plan.NameIn(PlatformLanguageInput.Read(request.Language)),
            version.CurrencyCode,
            offer.Term,
            offer.Licences,
            version.Offers(offer.Term) ? PlatformPricing.PeriodAmount(version, offer.Term, offer.Licences) : 0m,
            offer.Discount is null ? null : PlatformDiscountTermsItem.From(offer.Discount),
            offer.SetupFeeOverride ?? version.SetupFee,
            offer.Charges.Select(item => new PlatformOfferChargeItem(item.Description, item.Amount)).ToArray(),
            offer.StartTrial,
            version.TrialDays,
            offer.ExpiresUtc));
    }
}
