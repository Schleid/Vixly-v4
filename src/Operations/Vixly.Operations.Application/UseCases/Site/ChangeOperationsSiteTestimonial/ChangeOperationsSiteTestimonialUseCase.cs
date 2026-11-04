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
// - Customer reviews (owner, 2026-11-03): published only with the customer's agreement, hidden with a reason, never removed by the company; up and down order the published ones
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

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();

        try
        {
            switch (request.Action)
            {
                case "show":
                    IReadOnlyList<OperationsSiteTestimonial> shown = await _site.ListTestimonialsAsync(Vixly.Operations.Application.Site.OperationsTestimonialView.Published, 1000, cancellationToken);
                    testimonial.ChangeShown(true, utcNow);

                    if (testimonial.FromCustomer)
                    {
                        testimonial.MoveTo(shown.Where(item => item.Id != testimonial.Id).Select(item => item.SortOrder + 1).DefaultIfEmpty(0).Max(), utcNow);
                    }

                    break;
                case "hide":
                    testimonial.Hide(testimonial.FromCustomer ? request.HideReason : null, utcNow);
                    break;
                case "remove":
                    if (testimonial.FromCustomer)
                    {
                        return ChangeOperationsSiteTestimonialResponse.Failure(OperationsBrandingErrors.ReviewIsCustomers);
                    }

                    _site.RemoveTestimonial(testimonial);
                    break;
                case "up":
                case "down":
                    IReadOnlyList<OperationsSiteTestimonial> order = await _site.ListTestimonialsAsync(Vixly.Operations.Application.Site.OperationsTestimonialView.Published, 1000, cancellationToken);
                    int at = order.ToList().FindIndex(item => item.Id == testimonial.Id);
                    int other = request.Action == "up" ? at - 1 : at + 1;

                    if (at < 0 || other < 0 || other >= order.Count)
                    {
                        return ChangeOperationsSiteTestimonialResponse.Success();
                    }

                    // Number them in their current order first, so two with the same place still swap.
                    for (int index = 0; index < order.Count; index++)
                    {
                        order[index].MoveTo(index == at ? other : index == other ? at : index, utcNow);
                    }

                    break;
                default:
                    return ChangeOperationsSiteTestimonialResponse.Failure(OperationsCommonErrors.NotFound);
            }
        }
        catch (Vixly.Shared.Kernel.Domain.DomainException exception)
        {
            return ChangeOperationsSiteTestimonialResponse.Failure(exception.Error);
        }

        _audit.Record(context.UserId, "ops.site.testimonial_" + request.Action, nameof(OperationsSiteTestimonial), testimonial.Id,
            arguments: request.Action == "hide" && request.HideReason is { } reason ? new { reason = reason.ToString() } : null);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsSiteTestimonialResponse.Success()
            : ChangeOperationsSiteTestimonialResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
