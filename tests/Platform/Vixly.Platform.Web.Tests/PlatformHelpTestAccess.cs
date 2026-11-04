using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Domain.Identity;
using Vixly.Platform.Web.Security;

namespace Vixly.Platform.Web.Tests;

// =========================================================
// FILE: PlatformHelpTestAccess.cs
//
// PURPOSE:
// Authenticates a synthetic staff member in the test server only.
//
// RESPONSIBILITIES:
// - Provide trusted claim structure without a real session store.
//
// IMPORTANT NOTES:
// - This handler is never registered in a production host.
// =========================================================
internal sealed class PlatformHelpTestAccess : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public PlatformHelpTestAccess(IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
    {
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        ClaimsPrincipal user = new(new ClaimsIdentity(new[]
        {
            new Claim(PlatformClaimTypes.UserId, "aaaa0000-0000-0000-0000-000000000001"),
            new Claim(PlatformClaimTypes.SessionId, "aaaa0000-0000-0000-0000-000000000002"),
            new Claim(PlatformClaimTypes.Authority, ((int)PlatformUserAuthorityLevel.Standard).ToString(System.Globalization.CultureInfo.InvariantCulture)),
            new Claim(ClaimTypes.Name, "Demo staff")
        }, Scheme.Name));
        return Task.FromResult(AuthenticateResult.Success(new AuthenticationTicket(user, Scheme.Name)));
    }
}
