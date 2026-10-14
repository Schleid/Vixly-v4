namespace Vixly.Platform.Application.UseCases.Registration.CorrectRegistrationEmail;

// =========================================================
// FILE: CorrectRegistrationEmailRequest.cs
//
// PURPOSE:
// Input for CorrectRegistrationEmail.
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - The access token Public.Web holds
// =========================================================
public sealed class CorrectRegistrationEmailRequest
{
    public string AccessToken { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;
}
