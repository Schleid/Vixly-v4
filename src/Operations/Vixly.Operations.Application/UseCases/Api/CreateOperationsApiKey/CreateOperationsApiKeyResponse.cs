using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.CreateOperationsApiKey;

// =========================================================
// FILE: CreateOperationsApiKeyResponse.cs
//
// PURPOSE:
// The answer to createOperationsApiKey.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class CreateOperationsApiKeyResponse
{
    private CreateOperationsApiKeyResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public string Token { get; private init; } = string.Empty;

    public static CreateOperationsApiKeyResponse Success(string token)
    {
        return new CreateOperationsApiKeyResponse { Succeeded = true, Token = token };
    }

    public static CreateOperationsApiKeyResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new CreateOperationsApiKeyResponse { Error = error };
    }
}
