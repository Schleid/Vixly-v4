using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Branding;

namespace Vixly.Operations.Application.UseCases.Site.ListOperationsSiteTestimonials;

// =========================================================
// FILE: ListOperationsSiteTestimonialsUseCase.cs
//
// PURPOSE:
// The company's customer testimonials for the website editor (owner, 2026-10-30).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Those who view the website see every testimonial, shown or hidden
// - Customer reviews (owner, 2026-11-03): one view at a time, at most 200, with the number of new ones
// =========================================================
public sealed class ListOperationsSiteTestimonialsUseCase
{
    private const int MaxListed = 200;

    private readonly IOperationsSiteRepository _site;

    private readonly OperationsOwnerCheck _owner;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsPlanFeatures _features;

    public ListOperationsSiteTestimonialsUseCase(IOperationsSiteRepository site, OperationsOwnerCheck owner, IOperationsSettingsRepository settings, IOperationsPlanFeatures features)
    {
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _features = features ?? throw new ArgumentNullException(nameof(features));
    }

    public async Task<ListOperationsSiteTestimonialsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsSiteTestimonialsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BrandView, cancellationToken) is { } refused)
        {
            return ListOperationsSiteTestimonialsResponse.Failure(refused);
        }

        IReadOnlyList<OperationsSiteTestimonial> items = await _site.ListTestimonialsAsync(request.View, MaxListed, cancellationToken);
        bool portal = (await _features.GetAsync(context.RequiredTenantId, cancellationToken)).Includes(Vixly.Operations.Application.Plans.OperationsPlanModules.CustomerPortal);
        return ListOperationsSiteTestimonialsResponse.Success(items, await _site.CountNewReviewsAsync(cancellationToken),
            portal && (await _settings.FindAsync(cancellationToken))?.AskForReviews == true, portal);
    }
}
