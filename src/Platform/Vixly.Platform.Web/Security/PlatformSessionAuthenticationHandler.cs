using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Vixly.Platform.Application.Identity;

namespace Vixly.Platform.Web.Security;

// =========================================================
// FILE: PlatformSessionAuthenticationHandler.cs
//
// PURPOSE:
// Authenticates console requests from the server-side session
// (ADR-0006 section 6).
//
// RESPONSIBILITIES:
// - Read the session cookie and validate it against the
//   database
// - Build the principal from the database: user, session,
//   authority, display name
// - Delete the cookie when the session is no longer valid
// - Send unauthenticated users to /sign-in
//
// IMPORTANT NOTES:
// - One scheme for the whole process; no scheme guessing
//   (S-4)
// - Every request keeps the session alive (at most one write
//   a minute)
// =========================================================
public sealed class PlatformSessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "VixlySession";

    private readonly PlatformSessionValidator _validator;

    public PlatformSessionAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        PlatformSessionValidator validator)
        : base(options, logger, encoder)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Cookies.TryGetValue(PlatformCookies.Session, out string? token) || string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        PlatformValidatedSession? session = await _validator.ValidateWithUserAsync(token, Context.RequestAborted);

        if (session is null)
        {
            PlatformCookies.Delete(Response, PlatformCookies.Session);
            return AuthenticateResult.NoResult();
        }

        ClaimsIdentity identity = new(
            new[]
            {
                new Claim(PlatformClaimTypes.UserId, session.Context.PlatformUserId.ToString()),
                new Claim(PlatformClaimTypes.SessionId, session.Context.SessionId!.Value.ToString()),
                new Claim(PlatformClaimTypes.Authority, ((int)session.Context.AuthorityLevel).ToString(System.Globalization.CultureInfo.InvariantCulture)),
                new Claim(PlatformClaimTypes.DisplayName, session.DisplayName),
                new Claim(PlatformClaimTypes.Language, session.PreferredLanguage)
            },
            SchemeName,
            PlatformClaimTypes.DisplayName,
            null);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        string returnUrl = Request.PathBase + Request.Path + Request.QueryString;
        Response.Redirect("/sign-in?returnUrl=" + Uri.EscapeDataString(returnUrl));
        return Task.CompletedTask;
    }
}
