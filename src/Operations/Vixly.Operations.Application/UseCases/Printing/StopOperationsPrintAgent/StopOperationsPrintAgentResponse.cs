using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.StopOperationsPrintAgent;

// =========================================================
// FILE: StopOperationsPrintAgentResponse.cs
//
// PURPOSE:
// Stops a dock print program: its key no longer collects labels (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class StopOperationsPrintAgentResponse
{
    private StopOperationsPrintAgentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid AgentId { get; private init; }

    public static StopOperationsPrintAgentResponse Success(Guid agentId)
    {
        return new StopOperationsPrintAgentResponse { Succeeded = true, AgentId = agentId };
    }

    public static StopOperationsPrintAgentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new StopOperationsPrintAgentResponse { Error = error };
    }
}
