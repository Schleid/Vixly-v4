namespace Vixly.Operations.Application.UseCases.Api.CreateOperationsApiKey;

// =========================================================
// FILE: CreateOperationsApiKeyRequest.cs
//
// PURPOSE:
// Makes an API key for the company’s own programs (Slice 10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class CreateOperationsApiKeyRequest
{
    public string? Name { get; init; }
}
