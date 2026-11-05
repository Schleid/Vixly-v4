using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Vixly.Operations.Web.Hosting;

namespace Vixly.Operations.Web.Security;

// =========================================================
// FILE: OperationsStepUpContinuation.cs
//
// PURPOSE:
// A sensitive action that asked for a fresh code continues once the code is given (owner, 2026-11-04: "the invitation does not go through,
// I have to do it a 2nd time").
//
// RESPONSIBILITIES:
// - Keep the form of a staff action that was sent to the code page, in a cookie only this site can read
// - Tie it to that one code page (a random step id in its address), so a code entered for anything else never sends it
// - After a correct code on that page, give the action back once, to be sent again with a fresh form token
//
// IMPORTANT NOTES:
// - The cookie is encrypted and signed (Data Protection), bound to the session and the step id, kept 5 minutes; used once, checked here too
// - The old form token is never kept; the action is sent again through its endpoint, with every check (rights, fresh code, form guard)
// - Not kept, and any older kept action dropped: uploads, forms with secrets (keys, tokens, passwords), forms over 3,500 characters
//   once protected; those keep the old way (back to the page)
// =========================================================
public sealed class OperationsStepUpContinuation
{
    private const string Purpose = "Vixly.Operations.StepUpContinuation.v1";

    private const int MaxCookieLength = 3500;

    private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);

    // Review, 2026-11-04: secrets are never carried or written back into a page.
    private static readonly string[] SecretParts = { "secret", "password", "token", "apikey", "privatekey" };

    private readonly ITimeLimitedDataProtector _protector;

    private readonly IMemoryCache _used;

    public OperationsStepUpContinuation(IDataProtectionProvider provider, IMemoryCache used)
    {
        ArgumentNullException.ThrowIfNull(provider);
        _protector = provider.CreateProtector(Purpose).ToTimeLimitedDataProtector();
        _used = used ?? throw new ArgumentNullException(nameof(used));
    }

    // Called as the response starts: keeps the form when this action was sent to the code page; otherwise nothing older survives that page.
    public void KeepIfAsked(HttpContext http)
    {
        ArgumentNullException.ThrowIfNull(http);

        string location = http.Response.Headers.Location.ToString();

        if (http.Response.StatusCode != StatusCodes.Status302Found || !location.StartsWith("/staff/confirm?", StringComparison.Ordinal))
        {
            return;
        }

        if (http.Features.Get<IFormFeature>()?.Form is not { Files.Count: 0 } form
            || form.Keys.Any(IsSecret)
            || SessionOf(http) is not { } session
            || Action(http.Request.Path.ToUriComponent() + http.Request.QueryString.ToUriComponent()) is not { } action)
        {
            Drop(http);
            return;
        }

        string step = Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
        string[][] fields = form
            .Where(field => !string.Equals(field.Key, "__RequestVerificationToken", StringComparison.Ordinal))
            .SelectMany(field => field.Value.Select(value => new[] { field.Key, value ?? string.Empty }))
            .ToArray();
        string kept = _protector.Protect(JsonSerializer.Serialize(new Kept(step, action, session, fields)), Lifetime);

        if (kept.Length > MaxCookieLength)
        {
            Drop(http);
            return;
        }

        OperationsCookies.Write(http.Response, OperationsCookies.StepUp, kept, Lifetime);
        http.Response.Headers.Location = location + "&step=" + step;
    }

    // After a correct code on the page of this step: the kept action of this session, once. The cookie is removed whatever it holds.
    public OperationsStepUpAction? Take(HttpContext http, string? step)
    {
        ArgumentNullException.ThrowIfNull(http);

        if (!http.Request.Cookies.TryGetValue(OperationsCookies.StepUp, out string? value) || string.IsNullOrEmpty(value))
        {
            return null;
        }

        Drop(http);

        try
        {
            Kept? kept = JsonSerializer.Deserialize<Kept>(_protector.Unprotect(value));

            if (kept is null || string.IsNullOrEmpty(step) || !string.Equals(kept.Step, step, StringComparison.Ordinal)
                || SessionOf(http) is not { } session || kept.Session != session || Action(kept.Action) is not { } action)
            {
                return null;
            }

            // Once, also when two tabs send a correct code at the same moment.
            lock (_used)
            {
                if (_used.TryGetValue("ops.stepup:" + kept.Step, out _))
                {
                    return null;
                }

                _used.Set("ops.stepup:" + kept.Step, true, Lifetime);
            }

            return new OperationsStepUpAction(action, kept.Fields.Where(field => field.Length == 2).Select(field => (field[0], field[1])).ToArray());
        }
        catch (Exception exception) when (exception is CryptographicException or JsonException)
        {
            // Changed, expired or from another key ring: nothing to continue.
            return null;
        }
    }

    private static void Drop(HttpContext http)
    {
        if (http.Request.Cookies.ContainsKey(OperationsCookies.StepUp))
        {
            OperationsCookies.Delete(http.Response, OperationsCookies.StepUp);
        }
    }

    // The form's own token is never kept, so it is not a secret here.
    private static bool IsSecret(string name) =>
        !string.Equals(name, "__RequestVerificationToken", StringComparison.Ordinal) && SecretParts.Any(part => name.Replace("_", string.Empty, StringComparison.Ordinal).Contains(part, StringComparison.OrdinalIgnoreCase));

    private static Guid? SessionOf(HttpContext http) =>
        Guid.TryParse(http.User.FindFirstValue(OperationsClaimTypes.SessionId), out Guid session) ? session : null;

    // A staff action of this site, never the code page itself.
    private static string? Action(string? path) =>
        path is { Length: > 7 } && path.StartsWith("/staff/", StringComparison.Ordinal) && !path.StartsWith("/staff/confirm", StringComparison.Ordinal)
            && !path.StartsWith("//", StringComparison.Ordinal) && !path.Contains('\\', StringComparison.Ordinal) && !path.Contains("://", StringComparison.Ordinal)
            ? path
            : null;

    private sealed record Kept(string Step, string Action, Guid Session, string[][] Fields);
}
