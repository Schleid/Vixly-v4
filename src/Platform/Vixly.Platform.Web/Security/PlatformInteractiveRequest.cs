using System.Security.Claims;

namespace Vixly.Platform.Web.Security;

// =========================================================
// FILE: PlatformInteractiveRequest.cs
//
// PURPOSE:
// The request facts of an interactive console circuit: who is
// signed in, and the IP, browser and correlation id of the
// request that opened the circuit.
//
// RESPONSIBILITIES:
// - Hold the facts captured when the circuit starts (Routes)
// - Copy them into the private DI scope of a feature component
//   (PlatformPresenterComponent), where presenters read them
//
// IMPORTANT NOTES:
// - Interactive calls have no HttpContext, so audit entries and
//   authorization contexts read these instead (S-2)
// - Values come from the server-side request and the session
//   handler, never from the browser
// - Scoped: one per circuit, and one per component scope
// =========================================================
public sealed class PlatformInteractiveRequest
{
    public ClaimsPrincipal? User { get; private set; }

    public string? ClientIp { get; private set; }

    public string? UserAgent { get; private set; }

    public string? CorrelationId { get; private set; }

    public bool IsCaptured { get; private set; }

    public void Capture(string? clientIp, string? userAgent, string? correlationId)
    {
        ClientIp = clientIp;
        UserAgent = userAgent;
        CorrelationId = correlationId;
        IsCaptured = true;
    }

    public void CopyFrom(PlatformInteractiveRequest circuit, ClaimsPrincipal user)
    {
        ArgumentNullException.ThrowIfNull(circuit);
        ArgumentNullException.ThrowIfNull(user);

        Capture(circuit.ClientIp, circuit.UserAgent, circuit.CorrelationId);
        User = user;
    }
}
