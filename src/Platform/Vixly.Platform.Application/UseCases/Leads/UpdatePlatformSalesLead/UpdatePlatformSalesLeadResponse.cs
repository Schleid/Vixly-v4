using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Leads.UpdatePlatformSalesLead;

// =========================================================
// FILE: UpdatePlatformSalesLeadResponse.cs
//
// PURPOSE:
// Sales staff mark a contact request as contacted or closed, with a note (Stage 2).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class UpdatePlatformSalesLeadResponse
{
    private UpdatePlatformSalesLeadResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static UpdatePlatformSalesLeadResponse Success()
    {
        return new UpdatePlatformSalesLeadResponse { Succeeded = true };
    }

    public static UpdatePlatformSalesLeadResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new UpdatePlatformSalesLeadResponse { Error = error };
    }
}
