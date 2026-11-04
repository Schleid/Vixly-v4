using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Insurance;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Insurance.GetOperationsPolicyDocument;

// =========================================================
// FILE: GetOperationsPolicyDocumentResponse.cs
//
// PURPOSE:
// A document filed with an insurance policy, for staff (Slice 5B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsPolicyDocumentResponse
{
    private GetOperationsPolicyDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string ContentType { get; private init; } = string.Empty;

    public string FileName { get; private init; } = string.Empty;

    public static GetOperationsPolicyDocumentResponse Success(byte[] content, string contentType, string fileName)
    {
        return new GetOperationsPolicyDocumentResponse { Succeeded = true, Content = content, ContentType = contentType, FileName = fileName };
    }

    public static GetOperationsPolicyDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsPolicyDocumentResponse { Error = error };
    }
}
