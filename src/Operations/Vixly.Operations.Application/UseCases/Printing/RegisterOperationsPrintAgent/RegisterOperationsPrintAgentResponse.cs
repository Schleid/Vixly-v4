using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.RegisterOperationsPrintAgent;

// =========================================================
// FILE: RegisterOperationsPrintAgentResponse.cs
//
// PURPOSE:
// Registers the print program of a dock computer and gives its key once (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class RegisterOperationsPrintAgentResponse
{
    private RegisterOperationsPrintAgentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid AgentId { get; private init; }

    public string Key { get; private init; } = string.Empty;

    public static RegisterOperationsPrintAgentResponse Success(Guid agentId, string key)
    {
        return new RegisterOperationsPrintAgentResponse { Succeeded = true, AgentId = agentId, Key = key };
    }

    public static RegisterOperationsPrintAgentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new RegisterOperationsPrintAgentResponse { Error = error };
    }
}
