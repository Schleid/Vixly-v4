using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Security;

namespace Vixly.Operations.Application.UseCases.Site.GetOperationsPublicPricing;

// =========================================================
// FILE: GetOperationsPublicPricingUseCase.cs
//
// PURPOSE:
// The company's published prices on its public website (owner, 2026-10-21): the whole list on the Pricing page, only a yes or a no for the menu.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous: only regular lists the company publishes, while it publishes prices; never a customer's prices
// =========================================================
public sealed class GetOperationsPublicPricingUseCase
{
    private readonly OperationsPricing _pricing;

    public GetOperationsPublicPricingUseCase(
        OperationsPricing pricing)
    {
        _pricing = pricing ?? throw new ArgumentNullException(nameof(pricing));
    }

    public async Task<GetOperationsPublicPricingResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsPublicPricingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // The menu and the page answer the same question (review: a list not yet in force, or a service no longer offered, shows the page with nothing in force).
        bool shown = await _pricing.PublicShownAsync(cancellationToken);
        return GetOperationsPublicPricingResponse.Success(shown, shown && request.Full ? await _pricing.PublicAsync(context.RequiredTenantId, cancellationToken) : null);
    }
}
