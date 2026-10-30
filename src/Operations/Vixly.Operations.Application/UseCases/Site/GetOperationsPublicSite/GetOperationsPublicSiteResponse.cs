using Vixly.Operations.Application.Site;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Site.GetOperationsPublicSite;

// =========================================================
// FILE: GetOperationsPublicSiteResponse.cs
//
// PURPOSE:
// The published tenant site in one language, or the draft for the Owner (Slice 1B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsPublicSiteResponse
{
    private GetOperationsPublicSiteResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsPublicSite? Site { get; private init; }

    public static GetOperationsPublicSiteResponse Success(OperationsPublicSite? site)
    {
        return new GetOperationsPublicSiteResponse { Succeeded = true, Site = site };
    }

    public static GetOperationsPublicSiteResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsPublicSiteResponse { Error = error };
    }
}
