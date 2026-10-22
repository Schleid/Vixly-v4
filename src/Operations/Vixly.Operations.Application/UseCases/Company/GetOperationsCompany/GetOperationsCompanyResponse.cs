using Vixly.Operations.Application.Company;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.GetOperationsCompany;

// =========================================================
// FILE: GetOperationsCompanyResponse.cs
//
// PURPOSE:
// Reads the company's account at Vixly for the company page (Slice 1E).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCompanyResponse
{
    private GetOperationsCompanyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsCompanyView? View { get; private init; }

    public static GetOperationsCompanyResponse Success(OperationsCompanyView? view)
    {
        return new GetOperationsCompanyResponse { Succeeded = true, View = view };
    }

    public static GetOperationsCompanyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCompanyResponse { Error = error };
    }
}
