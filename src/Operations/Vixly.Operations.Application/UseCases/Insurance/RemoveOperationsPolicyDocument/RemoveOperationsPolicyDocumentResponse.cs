using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.RemoveOperationsPolicyDocument;

// =========================================================
// FILE: RemoveOperationsPolicyDocumentResponse.cs
//
// PURPOSE:
// Removes a document filed with an insurance policy (Slice 5B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveOperationsPolicyDocumentResponse
{
    private RemoveOperationsPolicyDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveOperationsPolicyDocumentResponse Success()
    {
        return new RemoveOperationsPolicyDocumentResponse { Succeeded = true };
    }

    public static RemoveOperationsPolicyDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveOperationsPolicyDocumentResponse { Error = error };
    }
}
