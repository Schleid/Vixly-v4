namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: VerifyPlatformRegistrationBody.cs
//
// PURPOSE:
// The token from the verification link.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Single use; 24 hours
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record VerifyPlatformRegistrationBody(string Token);
