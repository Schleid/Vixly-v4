using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Printing.PairOperationsPrintAgent;

// =========================================================
// FILE: PairOperationsPrintAgentResponse.cs
//
// PURPOSE:
// A phone running Vixly Print joins a warehouse with its one-time code and receives its print program key (Slice 12E, owner H-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class PairOperationsPrintAgentResponse
{
    private PairOperationsPrintAgentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public Guid AgentId { get; private init; }

    public string Key { get; private init; } = string.Empty;

    public string WarehouseName { get; private init; } = string.Empty;

    public static PairOperationsPrintAgentResponse Success(Guid agentId, string key, string warehouseName)
    {
        return new PairOperationsPrintAgentResponse { Succeeded = true, AgentId = agentId, Key = key, WarehouseName = warehouseName };
    }

    public static PairOperationsPrintAgentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new PairOperationsPrintAgentResponse { Error = error };
    }
}
