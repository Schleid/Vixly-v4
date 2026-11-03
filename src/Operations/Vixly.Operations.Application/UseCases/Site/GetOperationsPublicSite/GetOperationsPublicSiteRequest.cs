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

    // Website themes, step 1: in the Owner's preview only, shows the draft in another theme without saving it.
    public Vixly.Operations.Domain.Branding.OperationsSiteTheme? Theme { get; init; }

    // Landing page marketing (owner, 2026-10-30): only the home page reads packages delivered and testimonials.
    public bool Home { get; init; }
}
