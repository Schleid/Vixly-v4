namespace Vixly.Public.Web.Tests;

// =========================================================
// FILE: StubPlatformRequest.cs
//
// PURPOSE:
// One request the public site sent to the stand-in Platform.Api.
//
// RESPONSIBILITIES:
// - Carry the method, path, body and caller token
//
// IMPORTANT NOTES:
// - Test data only
// =========================================================
internal sealed record StubPlatformRequest(string Method, string Path, string Body, string? Caller);
