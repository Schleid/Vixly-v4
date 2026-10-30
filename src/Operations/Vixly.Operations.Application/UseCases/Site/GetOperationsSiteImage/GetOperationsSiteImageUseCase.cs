using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.UseCases.Site.GetOperationsSiteImage;

// =========================================================
// FILE: GetOperationsSiteImageUseCase.cs
//
// PURPOSE:
// One image of the tenant site (Slice 1B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Images never change: an id is cached for a year
// =========================================================
public sealed class GetOperationsSiteImageUseCase
{
    private readonly IOperationsSiteRepository _site;

    private readonly OperationsOwnerCheck _owner;

    public GetOperationsSiteImageUseCase(
        IOperationsSiteRepository site,
        OperationsOwnerCheck owner)
    {
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public async Task<GetOperationsSiteImageResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsSiteImageRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsTenantBrand? brand = await _site.FindBrandAsync(cancellationToken);
        OperationsSiteImage? image = brand is null ? null : await _site.GetImageAsync(request.ImageId, cancellationToken);

        // Visitors get published images only; the Owner also sees the draft ones (preview).
        bool visible = image is not null
            && (brand!.Published.Uses(image.Id)
                || (brand.Draft.Uses(image.Id) && await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BrandView, cancellationToken) is null));

        return visible
            ? GetOperationsSiteImageResponse.Success(image!.ContentType, image.Content, image.Sha256, brand!.Published.Uses(image.Id))
            : GetOperationsSiteImageResponse.Failure(OperationsBrandingErrors.ImageNotFound);
    }
}
