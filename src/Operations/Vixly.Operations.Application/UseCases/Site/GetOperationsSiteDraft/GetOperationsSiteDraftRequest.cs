namespace Vixly.Operations.Application.UseCases.Site.GetOperationsSiteDraft;

// =========================================================
// FILE: GetOperationsSiteDraftRequest.cs
//
// PURPOSE:
// The draft site for the Owner editor (Slice 1B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsSiteDraftRequest
{
    public string Language { get; init; } = "en";
}
