using Vixly.Operations.Application.Company;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.GetOperationsBilling;

// =========================================================
// FILE: GetOperationsBillingResponse.cs
//
// PURPOSE:
// Reads the plan, invoices and billing PDFs for the billing page (Slice 1E).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsBillingResponse
{
    private GetOperationsBillingResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsBillingView? View { get; private init; }

    public static GetOperationsBillingResponse Success(OperationsBillingView? view)
    {
        return new GetOperationsBillingResponse { Succeeded = true, View = view };
    }

    public static GetOperationsBillingResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsBillingResponse { Error = error };
    }
}
