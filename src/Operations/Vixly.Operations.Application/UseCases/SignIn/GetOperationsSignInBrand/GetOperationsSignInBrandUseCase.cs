using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Common;

namespace Vixly.Operations.Application.UseCases.SignIn.GetOperationsSignInBrand;

// =========================================================
// FILE: GetOperationsSignInBrandUseCase.cs
//
// PURPOSE:
// The name a tenant shows on its sign-in page (Slice 1).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous: the tenant comes from the host
// - Not found until the owner is invited; the page then shows no name
// =========================================================
public sealed class GetOperationsSignInBrandUseCase
{
    private readonly IOperationsBrandRepository _brands;

    public GetOperationsSignInBrandUseCase(
        IOperationsBrandRepository brands)
    {
        _brands = brands ?? throw new ArgumentNullException(nameof(brands));
    }

    public async Task<GetOperationsSignInBrandResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsSignInBrandRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        string? name = (await _brands.FindAsync(cancellationToken))?.DisplayName;

        return name is null
            ? GetOperationsSignInBrandResponse.Failure(OperationsCommonErrors.NotFound)
            : GetOperationsSignInBrandResponse.Success(name);
    }
}
