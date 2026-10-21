using Vixly.Platform.Application.Queries;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Leads.GetPlatformSalesLead;

// =========================================================
// FILE: GetPlatformSalesLeadResponse.cs
//
// PURPOSE:
// One contact request, and whether the reader may update it (Stage 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformSalesLeadResponse
{
    private GetPlatformSalesLeadResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformSalesLeadItem? Lead { get; private init; }

    public bool CanManage { get; private init; }

    public static GetPlatformSalesLeadResponse Success(PlatformSalesLeadItem? lead, bool canManage)
    {
        return new GetPlatformSalesLeadResponse { Succeeded = true, Lead = lead, CanManage = canManage };
    }

    public static GetPlatformSalesLeadResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformSalesLeadResponse { Error = error };
    }
}
