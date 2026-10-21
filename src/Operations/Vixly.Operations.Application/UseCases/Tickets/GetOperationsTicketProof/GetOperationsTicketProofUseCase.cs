using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Application.UseCases.Tickets.GetOperationsTicketProof;

// =========================================================
// FILE: GetOperationsTicketProofUseCase.cs
//
// PURPOSE:
// The carrier's proof of delivery attached to a ticket, for staff (Slice 4D step 4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.parcels.view for the ticket's warehouse; sent as a sandboxed download
// =========================================================
public sealed class GetOperationsTicketProofUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsTicketRepository _tickets;

    private readonly IOperationsFileStore _files;

    public GetOperationsTicketProofUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsTicketRepository tickets,
        IOperationsFileStore files)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _files = files ?? throw new ArgumentNullException(nameof(files));
    }

    public async Task<GetOperationsTicketProofResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsTicketProofRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsTicketProofResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _tickets.GetAsync(request.TicketId, cancellationToken) is not { ProofKey: { } key, ProofContentType: { } type } ticket
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, ticket.WarehouseId, cancellationToken) is null
            || await _files.ReadAsync(context.RequiredTenantId, key, cancellationToken) is not { } content)
        {
            return GetOperationsTicketProofResponse.Failure(OperationsTicketErrors.NotFound);
        }

        return GetOperationsTicketProofResponse.Success(content, type);
    }
}
