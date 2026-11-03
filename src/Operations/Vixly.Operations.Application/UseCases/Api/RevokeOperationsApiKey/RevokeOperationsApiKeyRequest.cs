namespace Vixly.Operations.Application.UseCases.Api.RevokeOperationsApiKey;

// =========================================================
// FILE: RevokeOperationsApiKeyRequest.cs
//
// PURPOSE:
// Revokes an API key at once (Slice 10).
//
// RESPONSIBILITIES:
// - Carry the caller's input
//
// IMPORTANT NOTES:
// - Validated by the use case and the domain
// =========================================================
public sealed class RevokeOperationsApiKeyRequest
{
    public Guid KeyId { get; init; }
}
