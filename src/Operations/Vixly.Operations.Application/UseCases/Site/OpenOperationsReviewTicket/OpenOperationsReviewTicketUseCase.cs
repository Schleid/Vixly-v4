using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Application.UseCases.Site.OpenOperationsReviewTicket;

// =========================================================
// FILE: OpenOperationsReviewTicketUseCase.cs
//
// PURPOSE:
// Opens a ticket from a customer review, for customer service to follow up (owner, 2026-11-03).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Topic Other, so customer service sees it; about the customer's last finished shipment
// - Never shown to the customer; the review itself stays as it is
// =========================================================
public sealed class OpenOperationsReviewTicketUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly IOperationsSiteRepository _site;

    private readonly IOperationsTicketRepository _tickets;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OpenOperationsReviewTicketUseCase(
        OperationsOwnerCheck owner,
        IOperationsSiteRepository site,
        IOperationsTicketRepository tickets,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<OpenOperationsReviewTicketResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        OpenOperationsReviewTicketRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BrandEdit, cancellationToken) is { } refused)
        {
            return OpenOperationsReviewTicketResponse.Failure(refused);
        }

        if (await _site.GetTestimonialAsync(request.TestimonialId, cancellationToken) is not { } review)
        {
            return OpenOperationsReviewTicketResponse.Failure(OperationsBrandingErrors.TestimonialNotFound);
        }

        if (review.CustomerId is not { } customerId)
        {
            return OpenOperationsReviewTicketResponse.Failure(OperationsBrandingErrors.ReviewNotFromCustomer);
        }

        if (await _site.FindLastFinishedShipmentAsync(customerId, cancellationToken) is not { } shipment)
        {
            return OpenOperationsReviewTicketResponse.Failure(OperationsBrandingErrors.ReviewNotYet);
        }

        Vixly.Operations.Domain.Tickets.OperationsTicket ticket;

        try
        {
            ticket = Vixly.Operations.Domain.Tickets.OperationsTicket.FromReview(context.RequiredTenantId, customerId, shipment.WarehouseId, shipment.ShipmentId, shipment.Reference,
                context.UserId ?? Guid.Empty, review.Rating + "/5 · " + review.Quote, _timeProvider.GetUtcNow());
        }
        catch (Vixly.Shared.Kernel.Domain.DomainException exception)
        {
            return OpenOperationsReviewTicketResponse.Failure(exception.Error);
        }

        _tickets.Add(ticket);
        _audit.Record(context.UserId, "ops.site.review_ticket_opened", nameof(OperationsSiteTestimonial), review.Id, arguments: new { ticket = ticket.Id });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? OpenOperationsReviewTicketResponse.Success(ticket.Id)
            : OpenOperationsReviewTicketResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
