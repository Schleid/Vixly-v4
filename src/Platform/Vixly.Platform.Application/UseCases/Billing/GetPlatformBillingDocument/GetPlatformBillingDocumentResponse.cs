using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Billing.GetPlatformBillingDocument;

// =========================================================
// FILE: GetPlatformBillingDocumentResponse.cs
//
// PURPOSE:
// Returns the stored PDF of an invoice, credit note or receipt (D-B10).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformBillingDocumentResponse
{
    private GetPlatformBillingDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public byte[] Content { get; private init; } = Array.Empty<byte>();

    public string FileName { get; private init; } = string.Empty;

    public static GetPlatformBillingDocumentResponse Success(byte[] content, string fileName)
    {
        return new GetPlatformBillingDocumentResponse { Succeeded = true, Content = content, FileName = fileName };
    }

    public static GetPlatformBillingDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformBillingDocumentResponse { Error = error };
    }
}
