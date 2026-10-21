using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.TenantAccount.SubmitTenantCompanyChange;

// =========================================================
// FILE: SubmitTenantCompanyChangeResponse.cs
//
// PURPOSE:
// Answer to a company change request (B12).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SubmitTenantCompanyChangeResponse
{
    private SubmitTenantCompanyChangeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid RequestId { get; private init; }

    public static SubmitTenantCompanyChangeResponse Success(Guid requestId)
    {
        return new SubmitTenantCompanyChangeResponse { Succeeded = true, RequestId = requestId };
    }

    public static SubmitTenantCompanyChangeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new SubmitTenantCompanyChangeResponse { Error = error };
    }
}
