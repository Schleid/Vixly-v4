namespace Vixly.Platform.Application.Ports;

// =========================================================
// FILE: IPlatformBotCheck.cs
//
// PURPOSE:
// Checks that a public form was sent by a person (D-T12).
//
// RESPONSIBILITIES:
// - Verify the bot-check token the public form produced
//
// IMPORTANT NOTES:
// - Implemented with Cloudflare Turnstile in Infrastructure
// - Called before any other work on the public registration
// =========================================================
public interface IPlatformBotCheck
{
    Task<bool> VerifyAsync(string? token, string? clientIp, CancellationToken cancellationToken);
}
