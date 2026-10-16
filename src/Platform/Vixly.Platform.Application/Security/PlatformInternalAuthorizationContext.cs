namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformInternalAuthorizationContext.cs
//
// PURPOSE:
// A call from Edge or Operations over the internal channel that is not about one tenant yet (portal lookup).
//
// RESPONSIBILITIES:
// - Mark a use case as internal-only
//
// IMPORTANT NOTES:
// - The channel itself is authenticated in Slice 9; browsers never reach it
// =========================================================
public sealed record PlatformInternalAuthorizationContext
{
    public static PlatformInternalAuthorizationContext Instance { get; } = new();

    private PlatformInternalAuthorizationContext()
    {
    }
}
