using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.CreateOperationsCounterShipment;

// =========================================================
// FILE: CreateOperationsCounterShipmentUseCase.cs
//
// PURPOSE:
// Creates a customer's shipment at the counter, with its parcel count (Slice 5C, C-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.counter.serve at the origin warehouse; only an active customer; the clerk never registers customers (owner)
// - Same rules as the portal (OperationsShipmentIntake); the service is required at the counter
// =========================================================
public sealed class CreateOperationsCounterShipmentUseCase
{
    private readonly OperationsCounterDesk _desk;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsShipmentIntake _intake;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsNumbering _numbering;

    private readonly OperationsCertificates _certificates;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CreateOperationsCounterShipmentUseCase(
        OperationsCounterDesk desk,
        IOperationsCustomerRepository customers,
        OperationsShipmentIntake intake,
        IOperationsReceivingRepository receiving,
        OperationsNumbering numbering,
        OperationsCertificates certificates,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CreateOperationsCounterShipmentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CreateOperationsCounterShipmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        ArgumentNullException.ThrowIfNull(request.Draft);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> counters, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return CreateOperationsCounterShipmentResponse.Failure(refused);
        }

        if (await _customers.GetAsync(request.CustomerId, cancellationToken) is not { } customer)
        {
            return CreateOperationsCounterShipmentResponse.Failure(OperationsReceivingErrors.CustomerNotFound);
        }

        if (customer.Status != OperationsCustomerStatus.Active)
        {
            return CreateOperationsCounterShipmentResponse.Failure(OperationsCustomerErrors.NotActive);
        }

        string? requestKey = Vixly.Operations.Application.Receiving.OperationsCommandKey.IsValid(request.RequestKey) ? request.RequestKey : null;

        // The same form sent twice: the shipment it created.
        if (requestKey is not null && await _receiving.FindShipmentByKeyAsync(customer.Id, requestKey, cancellationToken) is { } earlier)
        {
            return CreateOperationsCounterShipmentResponse.Success(earlier.Id);
        }

        // From the counter's receiving warehouse, where the customer stands with the parcels.
        if (!counters.Any(counter => counter.Id == request.WarehouseId && counter.IsOrigin))
        {
            return CreateOperationsCounterShipmentResponse.Failure(OperationsShipmentErrors.OriginInvalid);
        }

        // At the counter the service is chosen with the customer: the shipment is priced from it (C-6).
        if (string.IsNullOrWhiteSpace(request.Draft.Service))
        {
            return CreateOperationsCounterShipmentResponse.Failure(OperationsShipmentErrors.ServiceInvalid);
        }

        (OperationsShipmentInstructions? instructions, Error? invalid) = await _intake.InstructionsAsync(request.Draft, cancellationToken, customer.Id, access!.User.Id);

        if (instructions is null)
        {
            return CreateOperationsCounterShipmentResponse.Failure(invalid!);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsShipment shipment;

        try
        {
            shipment = OperationsShipment.Request(context.RequiredTenantId, customer.Id, request.WarehouseId, instructions, utcNow);
            shipment.OpenedAtCounter();
            shipment.DeclareParcelCount(request.ParcelCount, 0, utcNow);
        }
        catch (DomainException exception)
        {
            return CreateOperationsCounterShipmentResponse.Failure(exception.Error);
        }

        shipment.Keyed(requestKey);
        shipment.Numbered(await _numbering.NextAsync(OperationsNumberKind.Shipment, cancellationToken));
        _receiving.Add(shipment);

        // Slice 5B: insured, its certificate.
        await _certificates.SyncAsync(shipment, utcNow, cancellationToken);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CreateOperationsCounterShipmentResponse.Success(shipment.Id);
        }

        _unitOfWork.DiscardChanges();
        return requestKey is not null && await _receiving.FindShipmentByKeyAsync(customer.Id, requestKey, cancellationToken) is { } raced
            ? CreateOperationsCounterShipmentResponse.Success(raced.Id)
            : CreateOperationsCounterShipmentResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
