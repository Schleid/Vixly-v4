namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: PlatformRegistrationAccess.cs
//
// PURPOSE:
// The registration access token Public.Web keeps for the
// registrant.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Public.Web stores it in an HttpOnly cookie and sends it back
//   as a bearer token; Platform stores only its hash
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformRegistrationAccess(string AccessToken, DateTimeOffset ExpiresUtc, string? TenantCode);
