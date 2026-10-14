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

namespace Vixly.Platform.Application.UseCases.Registration.SelectRegistrationPlan;

// =========================================================
// FILE: SelectRegistrationPlanUseCase.cs
//
// PURPOSE:
// Records the plan a registering company chooses, and starts its subscription, with an offer link or a promotion code (D-S12, D-S20, D-S21).
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous, with the registration access token (Platform.Api)
// - With a trial, or with nothing to pay (free first period), the registration moves to account setup; otherwise it waits for payment (Slice 5)
// - An offer link fixes plan, term, licences and trial, may use a private plan, and must be used with the email it is tied to
// - Without an offer only public plans can be chosen; a promotion code works only when the Sales setting allows it
// =========================================================
public sealed class SelectRegistrationPlanUseCase
{
    private readonly PlatformRegistrationLinks _links;

    private readonly PlatformSubscriptionAdministration _administration;

    private readonly PlatformSalesAdministration _sales;

    private readonly PlatformAuditTrail _audit;

    private readonly IPlatformUnitOfWork _unitOfWork;

    public SelectRegistrationPlanUseCase(
        PlatformRegistrationLinks links,
        PlatformSubscriptionAdministration administration,
        PlatformSalesAdministration sales,
        PlatformAuditTrail audit,
        IPlatformUnitOfWork unitOfWork)
    {
        _links = links ?? throw new ArgumentNullException(nameof(links));
        _administration = administration ?? throw new ArgumentNullException(nameof(administration));
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SelectRegistrationPlanResponse> ExecuteAsync(
        PlatformAnonymousAuthorizationContext context,
        SelectRegistrationPlanRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        DateTimeOffset utcNow = _administration.UtcNow;
        PlatformRegistrationAccess access = await _links.OpenAsync(request.AccessToken, utcNow, cancellationToken);

        if (access.Registration is not PlatformTenantRegistration registration)
        {
            if (access.ExpiredNow)
            {
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return SelectRegistrationPlanResponse.Failure(access.Error!);
        }

        if (registration.Status != PlatformTenantRegistrationStatus.PendingPlanSelection || registration.PlatformTenantId is not Guid tenantId)
        {
            return SelectRegistrationPlanResponse.Failure(registration.Status == PlatformTenantRegistrationStatus.PendingEmailVerification
                ? PlatformRegistrationErrors.NotVerified
                : PlatformRegistrationErrors.PlanAlreadyChosen);
        }

        PlatformOffer? offer = null;

        if (!string.IsNullOrWhiteSpace(request.OfferToken))
        {
            offer = await _sales.Offers.GetByTokenHashAsync(_sales.HashToken(request.OfferToken.Trim()), cancellationToken);

            if (offer is null || offer.Refusal(registration.OwnerEmail.Value, utcNow) is not null)
            {
                return SelectRegistrationPlanResponse.Failure(offer?.Refusal(registration.OwnerEmail.Value, utcNow) ?? PlatformSalesErrors.OfferUnavailable);
            }
        }

        PlatformPlan? plan = offer is not null
            ? await _administration.Plans.GetAsync(offer.PlatformPlanId, cancellationToken)
            : string.IsNullOrWhiteSpace(request.PlanCode) ? null : await _administration.Plans.GetByCodeAsync(request.PlanCode.Trim().ToUpperInvariant(), cancellationToken);

        // An offer may use a private plan; without one only public plans can be chosen.
        if (plan is null || !plan.CanBeChosen || (offer is null && !plan.IsPublic))
        {
            return SelectRegistrationPlanResponse.Failure(PlatformSubscriptionErrors.PlanNotAvailable);
        }

        PlatformPromotion? promotion = null;

        if (offer is null && !string.IsNullOrWhiteSpace(request.PromotionCode))
        {
            if (!(await _sales.SettingsAsync(cancellationToken)).PromotionCodesAtRegistration)
            {
                return SelectRegistrationPlanResponse.Failure(PlatformSalesErrors.PromotionNotAtRegistration);
            }

            promotion = await _sales.Promotions.GetByCodeAsync(request.PromotionCode.Trim().ToUpperInvariant(), cancellationToken);

            if (promotion is null || promotion.Status is PlatformPromotionStatus.Draft or PlatformPromotionStatus.PendingApproval or PlatformPromotionStatus.Rejected)
            {
                return SelectRegistrationPlanResponse.Failure(PlatformSalesErrors.PromotionNotFound);
            }
        }

        if (await _administration.Subscriptions.GetCurrentAsync(tenantId, cancellationToken) is not null)
        {
            return SelectRegistrationPlanResponse.Failure(PlatformRegistrationErrors.PlanAlreadyChosen);
        }

        PlatformSubscriptionTerm term = offer?.Term ?? request.Term;
        int licences = offer?.Licences ?? request.Licences;
        bool trial = offer?.StartTrial ?? request.StartTrial;
        PlatformSubscription subscription;
        List<(PlatformSubscriptionEvent Event, string Action)> changes = new();

        try
        {
            PlatformSubscriptionStart start = PlatformSubscription.Start(
                tenantId,
                plan,
                term,
                licences,
                plan.Current!.Limit(PlatformPlanLimitKind.MaxLicensedDevices),
                trial,
                null,
                utcNow);
            subscription = start.Subscription;
            changes.Add((start.Event, "platform.subscriptions.started"));

            if (offer is not null)
            {
                // The salesperson who prepared the offer set these terms.
                if (offer.SetupFeeOverride is decimal fee)
                {
                    changes.Add((subscription.ChangeSetupFee(fee, plan.Current!, offer.Reason, offer.PreparedByUserId, utcNow), "platform.subscriptions.setup_fee_changed"));
                }

                foreach (PlatformOfferCharge charge in offer.Charges)
                {
                    changes.Add((subscription.AddCharge(charge.Description, charge.Amount, plan.CurrencyCode, offer.Reason, offer.PreparedByUserId, utcNow), "platform.subscriptions.charge_added"));
                }

                if (offer.Discount is not null)
                {
                    changes.Add((subscription.RequestDiscount(offer.Discount, PlatformDiscountSource.Offer, offer.Id, offer.Reason, false, offer.PreparedByUserId, utcNow), "platform.discounts.applied"));
                }

                offer.Use(registration.OwnerEmail.Value, utcNow);
            }

            if (promotion is not null)
            {
                promotion.Redeem(plan.Id, term, atRegistration: true, utcNow);
                changes.Add((subscription.RequestDiscount(promotion.Terms, PlatformDiscountSource.Promotion, promotion.Id, promotion.Code, false, null, utcNow), "platform.discounts.applied"));
            }

            PlatformSubscriptionEvent? activated = subscription.ActivateIfNothingDue(plan.Current!, utcNow);

            if (activated is not null)
            {
                changes.Add((activated, "platform.subscriptions.activated_nothing_due"));
            }

            registration.RecordPlanChosen(trial || activated is not null, utcNow);
        }
        catch (DomainException exception)
        {
            return SelectRegistrationPlanResponse.Failure(exception.Error);
        }

        _administration.Subscriptions.Add(subscription);

        foreach ((PlatformSubscriptionEvent change, string action) in changes)
        {
            _administration.RecordChange(null, subscription, plan, change, null, action);
        }

        _audit.Record(
            null,
            "platform.registrations.plan_chosen",
            null,
            nameof(PlatformTenantRegistration),
            registration.Id,
            after: new { plan = plan.Code, term = term.ToString(), licences, trial, offer = offer?.Id, promotion = promotion?.Code });

        if (offer is not null)
        {
            _audit.Record(null, "platform.offers.used", null, nameof(PlatformOffer), offer.Id, after: new { tenant = tenantId, status = offer.Status.ToString() });
        }

        if (promotion is not null)
        {
            _audit.Record(null, "platform.promotions.redeemed", null, nameof(PlatformPromotion), promotion.Id, after: new { tenant = tenantId });
        }

        if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SelectRegistrationPlanResponse.Failure(PlatformRegistrationErrors.PlanAlreadyChosen);
        }

        return SelectRegistrationPlanResponse.Success(registration.Status, subscription.TrialEndsUtc);
    }
}
