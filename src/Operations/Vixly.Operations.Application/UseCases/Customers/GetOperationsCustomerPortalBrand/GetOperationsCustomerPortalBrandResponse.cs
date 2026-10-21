using Vixly.Operations.Domain.Branding;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Customers.GetOperationsCustomerPortalBrand;

// =========================================================
// FILE: GetOperationsCustomerPortalBrandResponse.cs
//
// PURPOSE:
// The tenant's published look for the customer portal, after the plan's branding features (Slice 2, W-4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCustomerPortalBrandResponse
{
    private GetOperationsCustomerPortalBrandResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsBrandSettings? Settings { get; private init; }

    public static GetOperationsCustomerPortalBrandResponse Success(OperationsBrandSettings? settings)
    {
        return new GetOperationsCustomerPortalBrandResponse { Succeeded = true, Settings = settings };
    }

    public static GetOperationsCustomerPortalBrandResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCustomerPortalBrandResponse { Error = error };
    }
}
