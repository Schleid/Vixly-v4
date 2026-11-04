using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Site.SaveMyOperationsReview;

// =========================================================
// FILE: SaveMyOperationsReviewUseCase.cs
//
// PURPOSE:
// The customer writes or changes their review in the portal (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One review per customer, only after a shipment is delivered or picked up
// - A change makes it new again and takes it off the website until the company publishes it again
// - Without the customer's agreement it is feedback only and can never be published
// =========================================================
public sealed class SaveMyOperationsReviewUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsCustomerReviews _reviews;

    private readonly IOperationsSiteRepository _site;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public SaveMyOperationsReviewUseCase(
        OperationsCustomerAccounts accounts,
        OperationsCustomerReviews reviews,
        IOperationsSiteRepository site,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _reviews = reviews ?? throw new ArgumentNullException(nameof(reviews));
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<SaveMyOperationsReviewResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveMyOperationsReviewRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn || signedIn.Customer.TenantId != context.RequiredTenantId)
        {
            return SaveMyOperationsReviewResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        OperationsCustomer customer = signedIn.Customer;
        OperationsReviewState state = await _reviews.StateAsync(customer, cancellationToken);

        if (!state.Asked)
        {
            return SaveMyOperationsReviewResponse.Failure(OperationsBrandingErrors.ReviewsOff);
        }

        if (!state.Eligible)
        {
            return SaveMyOperationsReviewResponse.Failure(OperationsBrandingErrors.ReviewNotYet);
        }

        DateTimeOffset utcNow = _accounts.UtcNow;
        OperationsSiteTestimonial review;

        try
        {
            if (state.Review is { } existing)
            {
                existing.Rewrite(customer.FirstName, customer.LastName, request.NameStyle, request.Place, request.Quote, request.Rating, request.Agreed, utcNow);
                review = existing;
            }
            else
            {
                review = OperationsSiteTestimonial.Submit(context.RequiredTenantId, customer.Id, customer.FirstName, customer.LastName, request.NameStyle, request.Place,
                    request.Quote, request.Rating, request.Agreed, utcNow);
                _site.AddTestimonial(review);
            }
        }
        catch (DomainException exception)
        {
            _unitOfWork.DiscardChanges();
            return SaveMyOperationsReviewResponse.Failure(exception.Error);
        }

        _audit.Record(null, state.Review is null ? "ops.reviews.submitted" : "ops.reviews.changed", nameof(OperationsSiteTestimonial), review.Id,
            arguments: new { rating = review.Rating, agreed = review.CustomerAgreedUtc is not null });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? SaveMyOperationsReviewResponse.Success()
            : SaveMyOperationsReviewResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
