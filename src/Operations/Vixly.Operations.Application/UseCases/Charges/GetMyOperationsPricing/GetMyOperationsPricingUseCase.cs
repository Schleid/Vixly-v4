using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.GetMyOperationsPricing;

// =========================================================
// FILE: GetMyOperationsPricingUseCase.cs
//
// PURPOSE:
// The prices a signed-in customer sees on the portal's Pricing page (owner, 2026-10-21): the regular prices and their own.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Customer scope; never another customer's prices
// =========================================================
public sealed class GetMyOperationsPricingUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsPricing _pricing;

    public GetMyOperationsPricingUseCase(
        OperationsCustomerAccounts accounts,
        OperationsPricing pricing)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _pricing = pricing ?? throw new ArgumentNullException(nameof(pricing));
    }

    public async Task<GetMyOperationsPricingResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsPricingRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsPricingResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        return GetMyOperationsPricingResponse.Success(await _pricing.ForCustomerAsync(context.RequiredTenantId, signedIn.Customer.Id, cancellationToken));
    }
}
