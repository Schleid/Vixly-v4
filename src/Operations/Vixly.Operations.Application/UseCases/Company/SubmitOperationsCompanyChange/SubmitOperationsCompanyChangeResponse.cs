using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.SubmitOperationsCompanyChange;

// =========================================================
// FILE: SubmitOperationsCompanyChangeResponse.cs
//
// PURPOSE:
// Asks Vixly to change the company's legal details, with proof (B12, Owner only).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SubmitOperationsCompanyChangeResponse
{
    private SubmitOperationsCompanyChangeResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SubmitOperationsCompanyChangeResponse Success()
    {
        return new SubmitOperationsCompanyChangeResponse { Succeeded = true };
    }

    public static SubmitOperationsCompanyChangeResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SubmitOperationsCompanyChangeResponse { Error = error };
    }
}
