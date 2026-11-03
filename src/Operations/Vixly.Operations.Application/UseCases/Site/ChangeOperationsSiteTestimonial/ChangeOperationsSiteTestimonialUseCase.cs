using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Application.UseCases.Site.ChangeOperationsSiteTestimonial;

// =========================================================
// FILE: ChangeOperationsSiteTestimonialUseCase.cs
//
// PURPOSE:
// Shows, hides or removes a customer testimonial on the website (owner, 2026-10-30).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Shown and hidden take effect on the website at once; the testimonial is the customer's words, never edited
// =========================================================
public sealed class ChangeOperationsSiteTestimonialUseCase
{
    private readonly IOperationsSiteRepository _site;

    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsSiteTestimonialUseCase(IOperationsSiteRepository site, OperationsOwnerCheck owner, OperationsAuditTrail audit, IOperationsUnitOfWork unitOfWork, TimeProvider timeProvider)
    {
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsSiteTestimonialResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsSiteTestimonialRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BrandEdit, cancellationToken) is { } refused)
        {
            return ChangeOperationsSiteTestimonialResponse.Failure(refused);
        }

        if (await _site.GetTestimonialAsync(request.TestimonialId, cancellationToken) is not { } testimonial)
        {
            return ChangeOperationsSiteTestimonialResponse.Failure(OperationsBrandingErrors.TestimonialNotFound);
        }

        switch (request.Action)
        {
            case "show":
                testimonial.ChangeShown(true, _timeProvider.GetUtcNow());
                break;
            case "hide":
                testimonial.ChangeShown(false, _timeProvider.GetUtcNow());
                break;
            case "remove":
                _site.RemoveTestimonial(testimonial);
                break;
            default:
                return ChangeOperationsSiteTestimonialResponse.Failure(OperationsCommonErrors.NotFound);
        }

        _audit.Record(context.UserId, "ops.site.testimonial_" + request.Action, nameof(OperationsSiteTestimonial), testimonial.Id);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsSiteTestimonialResponse.Success()
            : ChangeOperationsSiteTestimonialResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
