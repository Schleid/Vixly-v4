using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.GetPlatformLegalDocument;

// =========================================================
// FILE: GetPlatformLegalDocumentResponse.cs
//
// PURPOSE:
// Reads a legal document version with its files, included versions and the reader's actions.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetPlatformLegalDocumentResponse
{
    private GetPlatformLegalDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public PlatformLegalDocumentDetail? Detail { get; private init; }

    public static GetPlatformLegalDocumentResponse Success(PlatformLegalDocumentDetail? detail)
    {
        return new GetPlatformLegalDocumentResponse { Succeeded = true, Detail = detail };
    }

    public static GetPlatformLegalDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetPlatformLegalDocumentResponse { Error = error };
    }
}
