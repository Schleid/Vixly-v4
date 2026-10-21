using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Leads.SubmitPlatformSalesLead;

// =========================================================
// FILE: SubmitPlatformSalesLeadResponse.cs
//
// PURPOSE:
// A visitor of the public site asks Vixly sales to get in touch (Stage 2, D-U3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SubmitPlatformSalesLeadResponse
{
    private SubmitPlatformSalesLeadResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SubmitPlatformSalesLeadResponse Success()
    {
        return new SubmitPlatformSalesLeadResponse { Succeeded = true };
    }

    public static SubmitPlatformSalesLeadResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SubmitPlatformSalesLeadResponse { Error = error };
    }
}
