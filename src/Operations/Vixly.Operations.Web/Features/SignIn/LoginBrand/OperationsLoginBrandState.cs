namespace Vixly.Operations.Web.Features.SignIn.LoginBrand;

// =========================================================
// FILE: OperationsLoginBrandState.cs
//
// PURPOSE:
// Immutable presentation state for the staff entry logo.
//
// RESPONSIBILITIES:
// - Carry the published same-origin image address
//
// IMPORTANT NOTES:
// - Null uses the company initials; no platform logo fallback
// =========================================================
public sealed record OperationsLoginBrandState(string? LogoUrl);
