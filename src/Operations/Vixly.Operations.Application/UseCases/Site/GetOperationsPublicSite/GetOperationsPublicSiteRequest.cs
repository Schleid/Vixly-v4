namespace Vixly.Operations.Application.UseCases.Site.GetOperationsPublicSite;

// =========================================================
// FILE: GetOperationsPublicSiteRequest.cs
//
// PURPOSE:
// The published tenant site in one language, or the draft for the Owner (Slice 1B).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class GetOperationsPublicSiteRequest
{
    public string Language { get; init; } = "en";

    public bool Preview { get; init; }
}
