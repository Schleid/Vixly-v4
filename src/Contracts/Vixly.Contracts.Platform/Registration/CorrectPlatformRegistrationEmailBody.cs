namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: CorrectPlatformRegistrationEmailBody.cs
//
// PURPOSE:
// A corrected owner email, before verification.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - A new link goes to the new address
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record CorrectPlatformRegistrationEmailBody(string Email);
