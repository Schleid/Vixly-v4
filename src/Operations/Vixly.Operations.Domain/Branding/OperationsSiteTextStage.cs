namespace Vixly.Operations.Domain.Branding;

// =========================================================
// FILE: OperationsSiteTextStage.cs
//
// PURPOSE:
// Whether a site text belongs to the draft or the published site.
//
// RESPONSIBILITIES:
// - Name the stages
//
// IMPORTANT NOTES:
// - Stored as int
// =========================================================
public enum OperationsSiteTextStage
{
    Draft = 0,
    Published = 1
}
