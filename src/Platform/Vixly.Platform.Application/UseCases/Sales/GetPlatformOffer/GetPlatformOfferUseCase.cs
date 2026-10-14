using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases;
using Vixly.Platform.Application.UseCases.Sales;
using Vixly.Platform.Domain.Sales;
using Vixly.Platform.Domain.Subscriptions;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Sales.GetPlatformOffer;

// =========================================================
// FILE: GetPlatformOfferUseCase.cs
//
// PURPOSE:
// Reads one offer link for its console page.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - CanDecide is false for the person who prepared it
// =========================================================
public sealed class GetPlatformOfferUseCase
{
    private readonly PlatformSalesAdministration _sales;

    private readonly IPlatformSubscriptionQueries _queries;

    private readonly IPlatformPlanRepository _plans;

    public GetPlatformOfferUseCase(
        PlatformSalesAdministration sales,
        IPlatformSubscriptionQueries queries,
        IPlatformPlanRepository plans)
    {
        _sales = sales ?? throw new ArgumentNullException(nameof(sales));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
        _plans = plans ?? throw new ArgumentNullException(nameof(plans));
    }

    public async Task<GetPlatformOfferResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformOfferRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        bool manage = await _sales.HoldsAsync(context, PlatformPermissions.OffersManage, cancellationToken);
        bool approve = await _sales.HoldsAsync(context, PlatformPermissions.DiscountsApprove, cancellationToken);
        // Open to whoever prepares offers or approves them; the gate refuses anyone else.
        if (!manage && !approve && await _sales.AuthorizeAsync(context, PlatformPermissions.OffersManage, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformOfferResponse.Failure(refused);
        }

        PlatformOffer? offer = await _sales.Offers.GetAsync(request.OfferId, cancellationToken);

        if (offer is null)
        {
            return GetPlatformOfferResponse.Failure(PlatformSalesErrors.OfferNotFound);
        }

        LanguageCode language = PlatformLanguageInput.Read(request.Language);
        PlatformPlan? plan = await _plans.GetAsync(offer.PlatformPlanId, cancellationToken);
        IReadOnlyDictionary<Guid, string> names = await _queries.StaffNamesAsync(
            new[] { offer.PreparedByUserId, offer.DecidedByUserId ?? Guid.Empty }.Where(id => id != Guid.Empty).Distinct(),
            cancellationToken);

        PlatformOfferDetail detail = new(
            offer.Id,
            offer.CompanyName,
            offer.BoundEmail,
            offer.Reason,
            offer.PlatformPlanId,
            plan?.Code ?? string.Empty,
            plan?.NameIn(language) ?? string.Empty,
            plan?.CurrencyCode ?? string.Empty,
            offer.Term,
            offer.Licences,
            offer.Discount is null ? null : PlatformDiscountTermsItem.From(offer.Discount),
            offer.SetupFeeOverride,
            offer.Charges.Select(item => new PlatformOfferChargeItem(item.Description, item.Amount)).ToArray(),
            offer.StartTrial,
            offer.ExpiresUtc,
            offer.UsesAllowed,
            offer.Uses,
            offer.Status,
            names.TryGetValue(offer.PreparedByUserId, out string? preparer) ? preparer : null,
            offer.DecidedByUserId is Guid decider && names.TryGetValue(decider, out string? decided) ? decided : null,
            offer.DecidedUtc,
            offer.DecisionReason,
            offer.IssuedUtc,
            offer.CreatedUtc);

        return GetPlatformOfferResponse.Success(
            detail,
            new PlatformSalesAbilities(manage, approve && offer.Status == PlatformOfferStatus.PendingApproval && offer.PreparedByUserId != context.PlatformUserId));
    }
}
