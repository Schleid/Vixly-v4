namespace Vixly.Operations.Application.UseCases.Inspection.CopyOperationsStartingList;

// =========================================================
// FILE: CopyOperationsStartingListRequest.cs
//
// PURPOSE:
// Copies the starting restricted-items list into an empty company list, with a rule that uses it (Slice 3C, D-R5).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CopyOperationsStartingListRequest
{
    public string Language { get; init; } = "en";
}
