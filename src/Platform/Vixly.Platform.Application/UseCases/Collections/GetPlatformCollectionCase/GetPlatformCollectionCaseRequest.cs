namespace Vixly.Platform.Application.UseCases.Collections.GetPlatformCollectionCase;

// =========================================================
// FILE: GetPlatformCollectionCaseRequest.cs
//
// PURPOSE:
// Reads a collections case: its overdue invoices, history and the actions the reader may take.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetPlatformCollectionCaseRequest
{
    public Guid CaseId { get; init; }
}
