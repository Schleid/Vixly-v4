using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Site.DismissMyOperationsReviewInvite;

// =========================================================
// FILE: DismissMyOperationsReviewInviteUseCase.cs
//
// PURPOSE:
// The customer closes the portal's invitation to review; it does not come back (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The review page stays in the menu
// =========================================================
public sealed class DismissMyOperationsReviewInviteUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public DismissMyOperationsReviewInviteUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<DismissMyOperationsReviewInviteResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        DismissMyOperationsReviewInviteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn || signedIn.Customer.TenantId != context.RequiredTenantId)
        {
            return DismissMyOperationsReviewInviteResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        if (signedIn.Customer.ReviewInviteDismissedUtc is not null)
        {
            return DismissMyOperationsReviewInviteResponse.Success();
        }

        signedIn.Customer.DismissReviewInvite(_accounts.UtcNow);
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? DismissMyOperationsReviewInviteResponse.Success()
            : DismissMyOperationsReviewInviteResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
