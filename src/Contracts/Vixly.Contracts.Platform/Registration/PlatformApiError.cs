namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: PlatformApiError.cs
//
// PURPOSE:
// An error on the wire: a stable code and text in the caller's
// language.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - The code never changes; the message follows Accept-Language
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformApiError(string Code, string Message);
