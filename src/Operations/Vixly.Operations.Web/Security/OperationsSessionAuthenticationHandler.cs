using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Vixly.Operations.Application.Identity;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Tenancy;

namespace Vixly.Operations.Web.Security;

// =========================================================
// FILE: OperationsSessionAuthenticationHandler.cs
//
// PURPOSE:
// Signs in a staff request from its session cookie (Slice 1, S-4).
//
// RESPONSIBILITIES:
// - Validate the session of the current tenant
// - Drop a cookie that no longer works
// - Send the anonymous to the sign-in page
//
// IMPORTANT NOTES:
// - One scheme per process (S-4)
// - No tenant (unknown host): never authenticates
// - A cookie from another tenant host finds no session (D-F1)
// - A session that ended (idle, expired, signed out elsewhere) sends the person to the plain sign-in page with the reason (owner, 2026-10-20)
// - A request the browser dropped while its session was checked ends quietly: it is not an error
// =========================================================
public sealed class OperationsSessionAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "OperationsSession";

    // Set when the request carried a session that no longer works, so the sign-in page can say why.
    public const string SessionEndedItem = "vx.session.ended";

    private readonly OperationsSessionValidator _validator;

    public OperationsSessionAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger, UrlEncoder encoder, OperationsSessionValidator validator)
        : base(options, logger, encoder)
    {
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (OperationsTenantMiddleware.Host(Context)?.TenantId is null
            || !Request.Cookies.TryGetValue(OperationsCookies.Session, out string? token) || string.IsNullOrEmpty(token))
        {
            return AuthenticateResult.NoResult();
        }

        OperationsValidatedSession? session;

        try
        {
            session = await _validator.ValidateAsync(token, Context.RequestAborted);
        }
        catch (OperationCanceledException) when (Context.RequestAborted.IsCancellationRequested)
        {
            // The browser went away (a redirect, a closed tab): nobody reads the answer.
            return AuthenticateResult.NoResult();
        }

        if (session is null)
        {
            OperationsCookies.Delete(Response, OperationsCookies.Session);
            Context.Items[SessionEndedItem] = true;
            return AuthenticateResult.NoResult();
        }

        ClaimsIdentity identity = new(
            new[]
            {
                new Claim(OperationsClaimTypes.UserId, session.UserId.ToString()),
                new Claim(OperationsClaimTypes.SessionId, session.SessionId.ToString()),
                new Claim(OperationsClaimTypes.DisplayName, session.DisplayName),
                new Claim(OperationsClaimTypes.Language, session.PreferredLanguage),
                new Claim(OperationsClaimTypes.Owner, session.IsOwner ? "1" : "0"),
                new Claim(OperationsClaimTypes.Administrator, session.IsAdministrator ? "1" : "0"),
                new Claim(OperationsClaimTypes.Setup, session.IsSetupSession ? "1" : "0")
            },
            SchemeName,
            OperationsClaimTypes.DisplayName,
            null);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), SchemeName));
    }

    protected override Task HandleChallengeAsync(AuthenticationProperties properties)
    {
        Response.Redirect(Context.Items.ContainsKey(SessionEndedItem) ? "/staff?ended=1" : "/staff");
        return Task.CompletedTask;
    }
}
