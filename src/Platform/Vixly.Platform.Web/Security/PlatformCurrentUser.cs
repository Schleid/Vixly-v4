using System.Globalization;
using System.Security.Claims;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;

namespace Vixly.Platform.Web.Security;

// =========================================================
// FILE: PlatformCurrentUser.cs
//
// PURPOSE:
// Gives presenters the trusted authorization context of the
// signed-in user (S-2).
//
// RESPONSIBILITIES:
// - Build PlatformAuthorizationContext from the principal the
//   session handler created
// - Presenters use GetContext; endpoints use
//   FromPrincipal(HttpContext.User)
//
// IMPORTANT NOTES:
// - Never reads ids from routes, queries or forms
// - Reads the principal copied into the component scope
//   (PlatformInteractiveRequest); there is no HttpContext in an
//   interactive call
// - Throws when nobody is signed in: protected pages are
//   behind [Authorize], so this is a programming error
// =========================================================
public sealed class PlatformCurrentUser
{
    private readonly PlatformInteractiveRequest _request;

    public PlatformCurrentUser(PlatformInteractiveRequest request)
    {
        _request = request ?? throw new ArgumentNullException(nameof(request));
    }

    public PlatformAuthorizationContext GetContext()
    {
        return FromPrincipal(_request.User ?? throw new InvalidOperationException("No signed-in Platform user."));
    }

    public static PlatformAuthorizationContext FromPrincipal(ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (!Guid.TryParse(user.FindFirst(PlatformClaimTypes.UserId)?.Value, out Guid userId)
            || !Guid.TryParse(user.FindFirst(PlatformClaimTypes.SessionId)?.Value, out Guid sessionId)
            || !int.TryParse(user.FindFirst(PlatformClaimTypes.Authority)?.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int authority))
        {
            throw new InvalidOperationException("No signed-in Platform user.");
        }

        return new PlatformAuthorizationContext(userId, (PlatformUserAuthorityLevel)authority, sessionId);
    }
}
