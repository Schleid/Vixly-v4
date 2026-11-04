using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Site.GetMyOperationsReview;

// =========================================================
// FILE: GetMyOperationsReviewUseCase.cs
//
// PURPOSE:
// The signed-in customer's review and whether they may write one (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Read-only; the customer sees only their own review
// =========================================================
public sealed class GetMyOperationsReviewUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsCustomerReviews _reviews;

    public GetMyOperationsReviewUseCase(
        OperationsCustomerAccounts accounts,
        OperationsCustomerReviews reviews)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _reviews = reviews ?? throw new ArgumentNullException(nameof(reviews));
    }

    public async Task<GetMyOperationsReviewResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsReviewRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn || signedIn.Customer.TenantId != context.RequiredTenantId)
        {
            return GetMyOperationsReviewResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        OperationsReviewState state = await _reviews.StateAsync(signedIn.Customer, cancellationToken);
        return GetMyOperationsReviewResponse.Success(state.Asked, state.Eligible, state.Review, state.InvitePending);
    }
}
