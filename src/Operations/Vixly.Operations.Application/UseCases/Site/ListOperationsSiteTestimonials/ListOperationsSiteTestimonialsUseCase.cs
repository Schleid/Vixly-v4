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
// =========================================================
public sealed class ListOperationsSiteTestimonialsUseCase
{
    private readonly IOperationsSiteRepository _site;

    private readonly OperationsOwnerCheck _owner;

    public ListOperationsSiteTestimonialsUseCase(IOperationsSiteRepository site, OperationsOwnerCheck owner)
    {
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
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

        return ListOperationsSiteTestimonialsResponse.Success(await _site.ListTestimonialsAsync(cancellationToken));
    }
}
