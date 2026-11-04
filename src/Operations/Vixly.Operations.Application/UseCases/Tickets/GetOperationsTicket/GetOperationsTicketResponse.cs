using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.GetOperationsTicket;

// =========================================================
// FILE: GetOperationsTicketResponse.cs
//
// PURPOSE:
// One ticket for staff: what the customer knows, the messages, and the undeclared packages that resemble it (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsTicketResponse
{
    private GetOperationsTicketResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsTicket? Ticket { get; private init; }

    public OperationsWarehouse? Warehouse { get; private init; }

    public string? CustomerCode { get; private init; }

    public string? CustomerName { get; private init; }

    public IReadOnlyList<OperationsParcelSummary> Lookalikes { get; private init; } = Array.Empty<OperationsParcelSummary>();

    public bool CanAnswer { get; private init; }

    // Ticket desk (owner, 2026-10-29): the agent who opened it, who took it, and the department's people it can be given to.
    public string? AgentName { get; private init; }

    public string? TakenByName { get; private init; }

    public IReadOnlyList<(Guid Id, string Name)> Handlers { get; private init; } = Array.Empty<(Guid, string)>();

    public static GetOperationsTicketResponse Success(OperationsTicket ticket, OperationsWarehouse warehouse, string? customerCode, string? customerName,
        IReadOnlyList<OperationsParcelSummary> lookalikes, bool canAnswer, string? agentName, string? takenByName, IReadOnlyList<(Guid Id, string Name)> handlers)
    {
        return new GetOperationsTicketResponse
        {
            Succeeded = true, Ticket = ticket, Warehouse = warehouse, CustomerCode = customerCode, CustomerName = customerName, Lookalikes = lookalikes, CanAnswer = canAnswer,
            AgentName = agentName, TakenByName = takenByName, Handlers = handlers
        };
    }

    public static GetOperationsTicketResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsTicketResponse { Error = error };
    }
}
