using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.AuthenticateOperationsApiKey;

// =========================================================
// FILE: AuthenticateOperationsApiKeyResponse.cs
//
// PURPOSE:
// The answer to authenticateOperationsApiKey.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class AuthenticateOperationsApiKeyResponse
{
    private AuthenticateOperationsApiKeyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string KeyId { get; private init; } = string.Empty;

    public string KeyName { get; private init; } = string.Empty;

    public static AuthenticateOperationsApiKeyResponse Success(string keyId, string keyName)
    {
        return new AuthenticateOperationsApiKeyResponse { Succeeded = true, KeyId = keyId, KeyName = keyName };
    }

    public static AuthenticateOperationsApiKeyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new AuthenticateOperationsApiKeyResponse { Error = error };
    }
}
