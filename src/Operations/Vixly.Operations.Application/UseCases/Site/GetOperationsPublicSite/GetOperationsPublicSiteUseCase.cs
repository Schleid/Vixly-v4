using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Site;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;

namespace Vixly.Operations.Application.UseCases.Site.GetOperationsPublicSite;

// =========================================================
// FILE: GetOperationsPublicSiteUseCase.cs
//
// PURPOSE:
// The published tenant site in one language, or the draft for the Owner (Slice 1B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous: the tenant comes from the host
// - Only the Owner sees the draft; anyone else asking for a preview gets the published site
// =========================================================
public sealed class GetOperationsPublicSiteUseCase
{
    private readonly IOperationsSiteRepository _site;

    private readonly IOperationsLocationRepository _locations;

    private readonly OperationsOwnerCheck _owner;

    private readonly IOperationsPlanFeatures _features;

    public GetOperationsPublicSiteUseCase(
        IOperationsSiteRepository site,
        IOperationsLocationRepository locations,
        OperationsOwnerCheck owner,
        IOperationsPlanFeatures features)
    {
        _features = features ?? throw new ArgumentNullException(nameof(features));
        _site = site ?? throw new ArgumentNullException(nameof(site));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
    }

    public async Task<GetOperationsPublicSiteResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsPublicSiteRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        OperationsTenantBrand? brand = await _site.FindBrandAsync(cancellationToken);

        if (brand is null)
        {
            return GetOperationsPublicSiteResponse.Failure(OperationsCommonErrors.NotFound);
        }

        bool preview = request.Preview && await _owner.RefuseUnlessAllowedAsync(context, Vixly.Operations.Domain.Access.OperationsPermissions.BrandView, cancellationToken) is null;
        OperationsSiteTextStage stage = preview ? OperationsSiteTextStage.Draft : OperationsSiteTextStage.Published;
        IReadOnlyList<OperationsBranch> branches = await _locations.ListBranchesAsync(cancellationToken);
        IReadOnlyList<OperationsPublicLocation> locations = branches
            .Where(branch => branch.IsPublic)
            .Select(branch => Location(branch.Id, "branch", branch.Name, branch.Address, branch.OpeningHours, OperationsWarehouseRoles.None, false))
            .Concat((await _locations.ListWarehousesAsync(cancellationToken))
                .Where(warehouse => warehouse.IsPublic)
                .Select(warehouse => Location(warehouse.Id, "warehouse", warehouse.Name, warehouse.Address, warehouse.OpeningHours, warehouse.Roles, warehouse.AcceptingDeliveries)))
            .OrderBy(location => location.CountryCode, StringComparer.Ordinal)
            .ThenBy(location => location.City, StringComparer.CurrentCulture)
            .ThenBy(location => location.Name, StringComparer.CurrentCulture)
            .ToList();
        IReadOnlyList<OperationsPublicContact> contacts = (await _locations.ListContactsAsync(cancellationToken))
            .OrderBy(contact => contact.SortOrder)
            .ThenBy(contact => contact.CountryCode, StringComparer.Ordinal)
            .Select(contact => new OperationsPublicContact(contact.CountryCode, contact.Label, contact.Phones, contact.WhatsApp, contact.Email, contact.Hours))
            .ToList();

        // What the plan does not include falls back to the defaults, for visitors and in the preview alike (W-4).
        OperationsPlanFeatures features = await _features.GetAsync(context.RequiredTenantId, cancellationToken);
        OperationsBrandSettings settings = preview ? brand.Draft : brand.Published;
        settings = features.ThemeCustomization ? settings : settings.WithDefaultTheme();
        settings = features.ContentCustomization ? settings : settings.WithDefaultContent();

        return GetOperationsPublicSiteResponse.Success(new OperationsPublicSite(
            settings,
            features.ContentCustomization ? brand.TextsFor(stage, request.Language) : new Dictionary<string, string>(StringComparer.Ordinal),
            brand.PublishedVersion,
            preview,
            locations,
            contacts));
    }

    private static OperationsPublicLocation Location(Guid id, string kind, string name, OperationsPostalAddress address, string? hours, OperationsWarehouseRoles roles, bool accepting)
    {
        return new OperationsPublicLocation(id, kind, name, address.Line1, address.Line2, address.City, address.Region, address.PostalCode, address.CountryCode, hours,
            roles.HasFlag(OperationsWarehouseRoles.Receiving), roles.HasFlag(OperationsWarehouseRoles.PickupPoint), accepting);
    }
}
