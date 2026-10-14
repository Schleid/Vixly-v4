namespace Vixly.Contracts.Platform.Registration;

// =========================================================
// FILE: PlatformOfferTokenBody.cs
//
// PURPOSE:
// The token of an offer link, sent in the body so it stays out of URLs and logs.
//
// RESPONSIBILITIES:
// - Carry data over HTTP between Public.Web and Platform.Api
//
// IMPORTANT NOTES:
// - Wire type: BCL only (C-0); never a domain type
// =========================================================
public sealed record PlatformOfferTokenBody
{
    public string Token { get; init; } = string.Empty;
}
