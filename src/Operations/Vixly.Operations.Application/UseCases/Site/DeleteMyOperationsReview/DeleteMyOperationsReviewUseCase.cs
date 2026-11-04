using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Site.DeleteMyOperationsReview;

// =========================================================
// FILE: DeleteMyOperationsReviewUseCase.cs
//
// PURPOSE:
// The customer deletes their review; it leaves the website at once (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Works even after the company stops asking for reviews
// - Nothing to delete is a success
// =========================================================
public sealed class DeleteMyOperationsReviewUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsSiteRepository _site;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public DeleteMyOperationsReviewUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsSiteRepository site,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<DeleteMyOperationsReviewResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        DeleteMyOperationsReviewRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn || signedIn.Customer.TenantId != context.RequiredTenantId)
        {
            return DeleteMyOperationsReviewResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        if (await _site.FindCustomerReviewAsync(signedIn.Customer.Id, cancellationToken) is not { } review)
        {
            return DeleteMyOperationsReviewResponse.Success();
        }

        _site.RemoveTestimonial(review);
        _audit.Record(null, "ops.reviews.deleted", nameof(OperationsSiteTestimonial), review.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? DeleteMyOperationsReviewResponse.Success()
            : DeleteMyOperationsReviewResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
