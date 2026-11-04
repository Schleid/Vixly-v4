using Vixly.Operations.Application.Api;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Api;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Api.SendOperationsWebhookTest;

// =========================================================
// FILE: SendOperationsWebhookTestResponse.cs
//
// PURPOSE:
// The answer to sendOperationsWebhookTest.
//
// RESPONSIBILITIES:
// - Say whether it succeeded, with what was asked or the refusal
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class SendOperationsWebhookTestResponse
{
    private SendOperationsWebhookTestResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public bool Delivered { get; private init; }

    public int? StatusCode { get; private init; }

    public string? Problem { get; private init; }

    public static SendOperationsWebhookTestResponse Success(bool delivered, int? statusCode, string? problem)
    {
        return new SendOperationsWebhookTestResponse { Succeeded = true, Delivered = delivered, StatusCode = statusCode, Problem = problem };
    }

    public static SendOperationsWebhookTestResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new SendOperationsWebhookTestResponse { Error = error };
    }
}
