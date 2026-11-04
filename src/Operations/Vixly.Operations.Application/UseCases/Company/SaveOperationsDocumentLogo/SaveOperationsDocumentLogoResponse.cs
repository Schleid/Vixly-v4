using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.SaveOperationsDocumentLogo;

// =========================================================
// FILE: SaveOperationsDocumentLogoResponse.cs
//
// PURPOSE:
// The answer to saving the document logo (owner, 2026-10-28).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SaveOperationsDocumentLogoResponse
{
    private SaveOperationsDocumentLogoResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static SaveOperationsDocumentLogoResponse Success() => new() { Succeeded = true };

    public static SaveOperationsDocumentLogoResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SaveOperationsDocumentLogoResponse { Error = error };
    }
}
