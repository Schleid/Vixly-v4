namespace Vixly.Platform.Web.Security;

// =========================================================
// FILE: PlatformClaimTypes.cs
//
// PURPOSE:
// Claim names of the signed-in principal.
//
// RESPONSIBILITIES:
// - Name the user id, session id, authority, display name and
//   preferred language
//
// IMPORTANT NOTES:
// - Claims are rebuilt from the database on every request;
//   nothing in them is trusted from the client
// =========================================================
public static class PlatformClaimTypes
{
    public const string UserId = "vx:uid";

    public const string SessionId = "vx:sid";

    public const string Authority = "vx:authority";

    public const string DisplayName = "vx:name";

    public const string Language = "vx:lang";
}
