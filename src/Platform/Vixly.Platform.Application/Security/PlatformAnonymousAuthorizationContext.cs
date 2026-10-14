namespace Vixly.Platform.Application.Security;

// =========================================================
// FILE: PlatformAnonymousAuthorizationContext.cs
//
// PURPOSE:
// The context of a use case that runs before anyone is signed
// in: sign-in, invitation acceptance, password reset.
//
// RESPONSIBILITIES:
// - Make "this use case is anonymous" explicit in its
//   signature (Global Standard 5.2: context first)
//
// IMPORTANT NOTES:
// - Carries no identity. Every anonymous endpoint has a
//   named rate-limit policy and is listed in the plane
//   document (S-6)
// - Built only by the host for its anonymous endpoints
// =========================================================
public sealed record PlatformAnonymousAuthorizationContext
{
    public static PlatformAnonymousAuthorizationContext Instance { get; } = new();

    private PlatformAnonymousAuthorizationContext()
    {
    }
}
