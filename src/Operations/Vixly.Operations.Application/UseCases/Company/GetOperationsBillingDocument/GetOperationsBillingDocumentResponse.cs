using Vixly.Operations.Application.Ports;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.GetOperationsBillingDocument;

// =========================================================
// FILE: GetOperationsBillingDocumentResponse.cs
//
// PURPOSE:
// Fetches one billing PDF for the Owner or an Administrator (Slice 1E).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsBillingDocumentResponse
{
    private GetOperationsBillingDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsLegalFile? File { get; private init; }

    public static GetOperationsBillingDocumentResponse Success(OperationsLegalFile? file)
    {
        return new GetOperationsBillingDocumentResponse { Succeeded = true, File = file };
    }

    public static GetOperationsBillingDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsBillingDocumentResponse { Error = error };
    }
}
