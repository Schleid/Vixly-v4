using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Holds;
using Vixly.Operations.Domain.Tickets;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Tickets.CreateMyOperationsTicket;

// =========================================================
// FILE: CreateMyOperationsTicketUseCase.cs
//
// PURPOSE:
// A customer opens a ticket about a package the carrier delivered that is not in its account (Slice 4D step 4, A-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Written by the customer only (D-C6)
// - The file is stored in the company's file store; removed again when the ticket is not saved
// =========================================================
public sealed class CreateMyOperationsTicketUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsShipmentIntake _intake;

    private readonly IOperationsTicketRepository _tickets;

    private readonly IOperationsFileStore _files;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public CreateMyOperationsTicketUseCase(
        OperationsCustomerAccounts accounts,
        OperationsShipmentIntake intake,
        IOperationsTicketRepository tickets,
        IOperationsFileStore files,
        IOperationsUnitOfWork unitOfWork)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
        _tickets = tickets ?? throw new ArgumentNullException(nameof(tickets));
        _files = files ?? throw new ArgumentNullException(nameof(files));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CreateMyOperationsTicketResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CreateMyOperationsTicketRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return CreateMyOperationsTicketResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        IReadOnlyList<OperationsChoice> origins = await _intake.OriginsAsync(signedIn.Customer, cancellationToken);
        Guid? warehouseId = request.WarehouseId ?? (origins.Count == 1 ? origins[0].Id : null);

        if (warehouseId is not { } warehouse || !origins.Any(origin => origin.Id == warehouse))
        {
            return CreateMyOperationsTicketResponse.Failure(Vixly.Operations.Domain.Shipments.OperationsShipmentErrors.OriginInvalid);
        }

        // The proof is checked by its first bytes, never its name (as the answers to holds, Slice 3E).
        string? type = null;

        if (request.Proof is { Length: > 0 } proof && (proof.Length > OperationsHoldLimits.MaxDocumentBytes || (type = Vixly.Operations.Application.Holds.OperationsDocumentInspector.TypeOf(proof)) is null))
        {
            return CreateMyOperationsTicketResponse.Failure(OperationsTicketErrors.ProofInvalid);
        }

        // One open ticket per package: the same report sent again (a double click, a second tab) opens the first one (ADR-0019).
        if (Vixly.Operations.Domain.Receiving.OperationsCarrierTracking.TryNormalize(request.Details.TrackingNumber, out string tracking)
            && await _tickets.FindOpenAsync(signedIn.Customer.Id, tracking, cancellationToken) is { } open)
        {
            return CreateMyOperationsTicketResponse.Success(open.Id);
        }

        DateTimeOffset utcNow = _accounts.UtcNow;
        OperationsTicket ticket;

        try
        {
            ticket = OperationsTicket.Open(context.RequiredTenantId, signedIn.Customer.Id, warehouse, request.Details, request.Message, utcNow);
        }
        catch (DomainException exception)
        {
            return CreateMyOperationsTicketResponse.Failure(exception.Error);
        }

        string? stored = null;
        bool saved = false;

        // The stored file is removed again whenever the ticket is not saved, even when saving throws (as the answers to holds).
        try
        {
            if (type is not null)
            {
                stored = await _files.SaveAsync(context.RequiredTenantId, "tickets", request.Proof!, type, cancellationToken);
                ticket.AttachProof(stored, type, Vixly.Operations.Application.Holds.OperationsDocumentInspector.Sha256(request.Proof!), utcNow);
            }

            _tickets.Add(ticket);
            saved = await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        finally
        {
            if (!saved && stored is not null)
            {
                await _files.DeleteAsync(context.RequiredTenantId, stored, CancellationToken.None);
            }
        }

        if (saved)
        {
            return CreateMyOperationsTicketResponse.Success(ticket.Id);
        }

        // Sent twice at once: the other request opened it (one open ticket per package).
        _unitOfWork.DiscardChanges();
        return await _tickets.FindOpenAsync(signedIn.Customer.Id, ticket.TrackingNumber, cancellationToken) is { } raced
            ? CreateMyOperationsTicketResponse.Success(raced.Id)
            : CreateMyOperationsTicketResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
    }
}
