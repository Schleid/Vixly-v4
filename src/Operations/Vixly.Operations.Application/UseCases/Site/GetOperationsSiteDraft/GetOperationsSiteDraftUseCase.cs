using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.UseCases.Site.GetOperationsSiteDraft;

// =========================================================
// FILE: GetOperationsSiteDraftUseCase.cs
//
// PURPOSE:
// The draft site for the Owner editor (Slice 1B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Owner only (Slice 1C brings ops.brand.view)
// =========================================================
public sealed class GetOperationsSiteDraftUseCase
{
    private readonly IOperationsSiteRepository _site;

    private readonly OperationsOwnerCheck _owner;

    public GetOperationsSiteDraftUseCase(
        IOperationsSiteRepository site,
        OperationsOwnerCheck owner)
    {
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public async Task<GetOperationsSiteDraftResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsSiteDraftRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BrandView, cancellationToken) is { } refused)
        {
            return GetOperationsSiteDraftResponse.Failure(refused);
        }

        OperationsTenantBrand? brand = await _site.FindBrandAsync(cancellationToken);

        return brand is null
            ? GetOperationsSiteDraftResponse.Failure(OperationsCommonErrors.NotFound)
            : GetOperationsSiteDraftResponse.Success(brand.Draft, brand.TextsFor(OperationsSiteTextStage.Draft, request.Language), brand.PublishedVersion, brand.HasUnpublishedChanges);
    }
}
