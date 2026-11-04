using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.AttachOperationsPolicyDocument;

// =========================================================
// FILE: AttachOperationsPolicyDocumentResponse.cs
//
// PURPOSE:
// Files a document with an insurance policy: the policy itself, an endorsement, a certificate (Slice 5B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AttachOperationsPolicyDocumentResponse
{
    private AttachOperationsPolicyDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static AttachOperationsPolicyDocumentResponse Success()
    {
        return new AttachOperationsPolicyDocumentResponse { Succeeded = true };
    }

    public static AttachOperationsPolicyDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AttachOperationsPolicyDocumentResponse { Error = error };
    }
}
