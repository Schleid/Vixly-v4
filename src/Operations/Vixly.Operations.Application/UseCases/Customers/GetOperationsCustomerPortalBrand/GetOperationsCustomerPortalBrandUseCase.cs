using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Branding;
using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Application.UseCases.Customers.GetOperationsCustomerPortalBrand;

// =========================================================
// FILE: GetOperationsCustomerPortalBrandUseCase.cs
//
// PURPOSE:
// The tenant's published look for the customer portal, after the plan's branding features (Slice 2, W-4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetOperationsCustomerPortalBrandUseCase
{
    private readonly IOperationsBrandRepository _brands;

    private readonly IOperationsPlanFeatures _features;

    public GetOperationsCustomerPortalBrandUseCase(
        IOperationsBrandRepository brands,
        IOperationsPlanFeatures features)
    {
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
        _features = features ?? throw new ArgumentNullException(nameof(features));
    }

    public async Task<GetOperationsCustomerPortalBrandResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCustomerPortalBrandRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _brands.FindAsync(cancellationToken) is not { } brand)
        {
            return GetOperationsCustomerPortalBrandResponse.Failure(OperationsCommonErrors.NotFound);
        }

        OperationsPlanFeatures features = await _features.GetAsync(context.RequiredTenantId, cancellationToken);
        OperationsBrandSettings settings = features.ThemeCustomization ? brand.Published : brand.Published.WithDefaultTheme();
        return GetOperationsCustomerPortalBrandResponse.Success(settings);
    }
}
