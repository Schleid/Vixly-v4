using System.Security.Claims;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using Vixly.Operations.Web.Hosting;
using Vixly.Operations.Web.Security;

namespace Vixly.Operations.Web.Tests;

// =========================================================
// FILE: OperationsStepUpContinuationTests.cs
//
// PURPOSE:
// Proves what a waiting staff action may carry and to whom it is given back (owner, 2026-11-04).
//
// RESPONSIBILITIES:
// - Kept only for a staff action sent to the code page, never its old form token
// - Given back once (also from a copied cookie), only to the same session and only to the code page of its step; a changed cookie gives nothing
// - Uploads, forms with secrets, the code page itself and other addresses are not kept, and drop anything kept before
//
// IMPORTANT NOTES:
// - Request contexts built by hand; the cookie travels as a browser would send it
// =========================================================
public sealed class OperationsStepUpContinuationTests
{
    private static readonly Guid Session = Guid.NewGuid();

    private readonly OperationsStepUpContinuation _continuation = new(new EphemeralDataProtectionProvider(), new MemoryCache(Options.Create(new MemoryCacheOptions())));

    [Fact]
    public void KeptForItsSessionOnly_Once_WithoutTheOldToken()
    {
        (string cookie, string step) = Keep("/staff/people/invite", Session, ("fullName", "Henry Louis"), ("roleId", "r1"), ("__RequestVerificationToken", "old-token"), ("permissions", "a"), ("permissions", "b"))!.Value;

        Assert.Null(_continuation.Take(Request(cookie, Guid.NewGuid()), step));
        Assert.Null(_continuation.Take(Request(cookie, Session), null));
        Assert.Null(_continuation.Take(Request(cookie, Session), new string('0', 32)));

        OperationsStepUpAction action = _continuation.Take(Request(cookie, Session), step)!;
        Assert.Equal("/staff/people/invite", action.Action);
        Assert.Equal(new[] { ("fullName", "Henry Louis"), ("roleId", "r1"), ("permissions", "a"), ("permissions", "b") }, action.Fields);
        Assert.DoesNotContain(action.Fields, field => field.Name == "__RequestVerificationToken");

        // The same cookie again (a second tab, a copy): nothing.
        Assert.Null(_continuation.Take(Request(cookie, Session), step));

        // Changed by hand: nothing.
        Assert.Null(_continuation.Take(Request(cookie[..^4] + "AAAA", Session), step));
    }

    [Fact]
    public void NotKept_ForUploads_Secrets_TheCodePage_OtherAddresses_OrActionsThatDidNotAsk()
    {
        Assert.Null(Keep("/staff/company/logo", Session, upload: true));
        Assert.Null(Keep("/staff/settings/payments", Session, ("secretKey", "sk_live_x")));
        Assert.Null(Keep("/staff/settings/webhooks", Session, ("webhook_secret", "whsec_x")));
        Assert.Null(Keep("/staff/settings/sms", Session, ("authToken", "t")));
        Assert.Null(Keep("/staff/account/password", Session, ("newPassword", "p")));
        Assert.Null(Keep("/staff/confirm", Session));
        Assert.Null(Keep("/customer/profile", Session));
        Assert.Null(Keep("/staff/people/invite", Session, location: "/staff/people/1?done=invited"));
        Assert.Null(Keep("/staff/people/invite", null));
    }

    [Fact]
    public void AnActionThatCannotBeKept_DropsTheOneKeptBefore()
    {
        (string cookie, string _) = Keep("/staff/people/invite", Session, ("fullName", "Henry Louis"))!.Value;

        Assert.Equal(string.Empty, Dropped("/staff/company/logo", cookie, upload: true));
        Assert.Equal(string.Empty, Dropped("/staff/settings/payments", cookie, fields: ("secretKey", "sk_live_x")));
    }

    [Fact]
    public void TheStepGoesIntoTheCodePageAddress()
    {
        DefaultHttpContext http = Asked("/staff/people/invite", Session, false, "/staff/confirm?returnUrl=%2Fstaff", null, ("fullName", "Henry Louis"));

        Assert.Matches("^/staff/confirm\\?returnUrl=%2Fstaff&step=[0-9a-f]{32}$", http.Response.Headers.Location.ToString());
    }

    private (string Cookie, string Step)? Keep(string path, Guid? session, params (string Name, string Value)[] fields) => Keep(path, session, false, "/staff/confirm?returnUrl=%2Fstaff", fields);

    private (string Cookie, string Step)? Keep(string path, Guid? session, bool upload = false, string location = "/staff/confirm?returnUrl=%2Fstaff", params (string Name, string Value)[] fields)
    {
        DefaultHttpContext http = Asked(path, session, upload, location, null, fields);
        string? set = http.Response.Headers.SetCookie.FirstOrDefault(header => header?.StartsWith(OperationsCookies.StepUp + "=", StringComparison.Ordinal) == true);
        string? cookie = set?.Split(';')[0][(OperationsCookies.StepUp.Length + 1)..];
        return string.IsNullOrEmpty(cookie) ? null : (cookie, http.Response.Headers.Location.ToString().Split("&step=")[1]);
    }

    // The value the response sets for the kept cookie: empty when it is removed, null when untouched.
    private string? Dropped(string path, string before, bool upload = false, params (string Name, string Value)[] fields)
    {
        DefaultHttpContext http = Asked(path, Session, upload, "/staff/confirm?returnUrl=%2Fstaff", before, fields);
        string? set = http.Response.Headers.SetCookie.FirstOrDefault(header => header?.StartsWith(OperationsCookies.StepUp + "=", StringComparison.Ordinal) == true);
        return set?.Split(';')[0][(OperationsCookies.StepUp.Length + 1)..];
    }

    private DefaultHttpContext Asked(string path, Guid? session, bool upload, string location, string? before, params (string Name, string Value)[] fields)
    {
        DefaultHttpContext http = new();

        if (before is not null)
        {
            http.Request.Headers.Cookie = OperationsCookies.StepUp + "=" + before;
        }

        http.Request.Method = "POST";
        http.Request.Path = path;
        http.User = User(session);
        Dictionary<string, StringValues> values = fields.GroupBy(field => field.Name).ToDictionary(group => group.Key, group => new StringValues(group.Select(field => field.Value).ToArray()));
        FormFileCollection files = new();

        if (upload)
        {
            files.Add(new FormFile(Stream.Null, 0, 0, "file", "logo.png"));
        }

        http.Features.Set<IFormFeature>(new FormFeature(new FormCollection(values, files)));
        http.Response.StatusCode = StatusCodes.Status302Found;
        http.Response.Headers.Location = location;
        _continuation.KeepIfAsked(http);
        return http;
    }

    private static HttpContext Request(string cookie, Guid session)
    {
        DefaultHttpContext http = new();
        http.Request.Headers.Cookie = OperationsCookies.StepUp + "=" + cookie;
        http.User = User(session);
        return http;
    }

    private static ClaimsPrincipal User(Guid? session) =>
        new(new ClaimsIdentity(session is { } id ? new[] { new Claim(OperationsClaimTypes.SessionId, id.ToString()) } : Array.Empty<Claim>(), "test"));
}
