using Vixly.Shared.Kernel.Errors;

namespace Vixly.Platform.Application.UseCases.Legal.PublishPlatformLegalDocument;

// =========================================================
// FILE: PublishPlatformLegalDocumentResponse.cs
//
// PURPOSE:
// Publishes a draft: it becomes the current version of its kind, never changed again (D-Lg3).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PublishPlatformLegalDocumentResponse
{
    private PublishPlatformLegalDocumentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static PublishPlatformLegalDocumentResponse Success()
    {
        return new PublishPlatformLegalDocumentResponse { Succeeded = true };
    }

    public static PublishPlatformLegalDocumentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PublishPlatformLegalDocumentResponse { Error = error };
    }
}
