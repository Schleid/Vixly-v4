namespace Vixly.Platform.Application.UseCases.Collections.AddPlatformCollectionNote;

// =========================================================
// FILE: AddPlatformCollectionNoteRequest.cs
//
// PURPOSE:
// Adds an internal note to a collections case (D-C8).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AddPlatformCollectionNoteRequest
{
    public Guid CaseId { get; init; }

    public string? Note { get; init; }
}
