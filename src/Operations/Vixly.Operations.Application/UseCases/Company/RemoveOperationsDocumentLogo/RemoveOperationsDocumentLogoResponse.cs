using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.RemoveOperationsDocumentLogo;

// =========================================================
// FILE: RemoveOperationsDocumentLogoResponse.cs
//
// PURPOSE:
// The answer to removing the document logo (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RemoveOperationsDocumentLogoResponse
{
    private RemoveOperationsDocumentLogoResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RemoveOperationsDocumentLogoResponse Success() => new() { Succeeded = true };

    public static RemoveOperationsDocumentLogoResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RemoveOperationsDocumentLogoResponse { Error = error };
    }
}
