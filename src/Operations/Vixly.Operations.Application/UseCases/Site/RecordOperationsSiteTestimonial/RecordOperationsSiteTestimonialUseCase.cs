using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Site.RecordOperationsSiteTestimonial;

// =========================================================
// FILE: RecordOperationsSiteTestimonialUseCase.cs
//
// PURPOSE:
// Records a customer's testimonial for the website, with the customer's agreement (owner, 2026-10-30).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Refused without the customer's agreement; the person who confirms it is recorded with the time
// =========================================================
public sealed class RecordOperationsSiteTestimonialUseCase
{
    private readonly IOperationsSiteRepository _site;

    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public RecordOperationsSiteTestimonialUseCase(IOperationsSiteRepository site, OperationsOwnerCheck owner, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<RecordOperationsSiteTestimonialResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        RecordOperationsSiteTestimonialRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BrandEdit, cancellationToken) is { } refused)
        {
            return RecordOperationsSiteTestimonialResponse.Failure(refused);
        }

        // The limit is for testimonials staff enter; customer reviews are one per customer (owner, 2026-11-03).
        IReadOnlyList<OperationsSiteTestimonial> existing = await _site.ListTestimonialsAsync(Vixly.Operations.Application.Site.OperationsTestimonialView.Staff, OperationsSiteTestimonial.MaxPerCompany + 1, cancellationToken);

        if (existing.Count >= OperationsSiteTestimonial.MaxPerCompany)
        {
            return RecordOperationsSiteTestimonialResponse.Failure(OperationsBrandingErrors.TestimonialLimitReached);
        }

        OperationsSiteTestimonial testimonial;

        try
        {
            testimonial = OperationsSiteTestimonial.Record(context.RequiredTenantId, request.Name, request.Place, request.Quote, request.Rating, request.Source,
                request.CustomerAgreed, context.UserId ?? Guid.Empty, existing.Count == 0 ? 0 : existing.Max(item => item.SortOrder) + 1, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return RecordOperationsSiteTestimonialResponse.Failure(exception.Error);
        }

        _site.AddTestimonial(testimonial);
        _audit.Record(context.UserId, "ops.site.testimonial_recorded", nameof(OperationsSiteTestimonial), testimonial.Id, arguments: new { consent = true });

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? RecordOperationsSiteTestimonialResponse.Success(testimonial.Id)
            : RecordOperationsSiteTestimonialResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
