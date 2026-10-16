namespace Vixly.Platform.Api.Internal;

// =========================================================
// FILE: PlatformInternalScopes.cs
//
// PURPOSE:
// What each internal route needs from the caller (Slice 9, D-K8).
//
// RESPONSIBILITIES:
// - Name the scopes
//
// IMPORTANT NOTES:
// - portal: Edge resolves tenants; tenant: Operations for one tenant; events: the feed; jobs: batch reads
// =========================================================
public static class PlatformInternalScopes
{
    public const string Portal = "portal";

    public const string Tenant = "tenant";

    public const string Events = "events";

    public const string Jobs = "jobs";
}
