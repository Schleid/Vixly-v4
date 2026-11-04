using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.RevokeOperationsApiKey;

// =========================================================
// FILE: RevokeOperationsApiKeyResponse.cs
//
// PURPOSE:
// The answer to revokeOperationsApiKey.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RevokeOperationsApiKeyResponse
{
    private RevokeOperationsApiKeyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public static RevokeOperationsApiKeyResponse Success()
    {
        return new RevokeOperationsApiKeyResponse { Succeeded = true };
    }

    public static RevokeOperationsApiKeyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RevokeOperationsApiKeyResponse { Error = error };
    }
}
