using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.GetTenantCompanyChange;

// =========================================================
// FILE: GetTenantCompanyChangeResponse.cs
//
// PURPOSE:
// The tenant’s latest company change request, if any (B12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetTenantCompanyChangeResponse
{
    private GetTenantCompanyChangeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformTenantChangeItem? Change { get; private init; }

    public static GetTenantCompanyChangeResponse Success(PlatformTenantChangeItem? change)
    {
        return new GetTenantCompanyChangeResponse { Succeeded = true, Change = change };
    }

    public static GetTenantCompanyChangeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new GetTenantCompanyChangeResponse { Error = error };
    }
}
