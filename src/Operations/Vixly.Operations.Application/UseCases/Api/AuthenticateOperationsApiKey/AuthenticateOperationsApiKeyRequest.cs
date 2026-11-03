namespace Vixly.Operations.Application.UseCases.Api.AuthenticateOperationsApiKey;

// =========================================================
// FILE: AuthenticateOperationsApiKeyRequest.cs
//
// PURPOSE:
// Checks a company API key on a request (Slice 10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class AuthenticateOperationsApiKeyRequest
{
    // The bearer token as sent.
    public string? Token { get; init; }
}
