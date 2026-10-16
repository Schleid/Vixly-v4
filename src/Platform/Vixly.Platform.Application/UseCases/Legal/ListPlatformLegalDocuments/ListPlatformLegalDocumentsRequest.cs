using Vixly.Platform.Domain.Legal;

namespace Vixly.Platform.Application.UseCases.Legal.ListPlatformLegalDocuments;

// =========================================================
// FILE: ListPlatformLegalDocumentsRequest.cs
//
// PURPOSE:
// Lists legal document versions, newest first, optionally of one kind.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class ListPlatformLegalDocumentsRequest
{
    public PlatformLegalDocumentKind? Kind { get; init; }

    public int Page { get; init; } = 1;

    public int PageSize { get; init; } = 25;
}
