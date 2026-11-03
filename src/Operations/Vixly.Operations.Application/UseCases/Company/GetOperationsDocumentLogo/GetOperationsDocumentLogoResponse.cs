using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Company.GetOperationsDocumentLogo;

// =========================================================
// FILE: GetOperationsDocumentLogoResponse.cs
//
// PURPOSE:
// The document logo, what it is and who put it there (owner, 2026-10-28); empty when there is none.
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsDocumentLogoResponse
{
    private GetOperationsDocumentLogoResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    // Null when the company has no document logo.
    public byte[]? Content { get; private init; }

    public string ContentType { get; private init; } = "image/png";

    public int Width { get; private init; }

    public int Height { get; private init; }

    public int Bytes { get; private init; }

    public DateTimeOffset? ChangedUtc { get; private init; }

    public string? ChangedBy { get; private init; }

    public bool CanEdit { get; private init; }

    public static GetOperationsDocumentLogoResponse Success(byte[]? content, int width, int height, int bytes, DateTimeOffset? changedUtc, string? changedBy, bool canEdit) => new() { Succeeded = true, Content = content, Width = width, Height = height, Bytes = bytes, ChangedUtc = changedUtc, ChangedBy = changedBy, CanEdit = canEdit };

    public static GetOperationsDocumentLogoResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsDocumentLogoResponse { Error = error };
    }
}
