using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterCustomer;

// =========================================================
// FILE: GetOperationsCounterCustomerUseCase.cs
//
// PURPOSE:
// Shows a customer at the counter: who they are, all their shipments, and what a new shipment can be (Slice 5C, C-5 and C-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.counter.serve; the clerk sees all the customer's shipments (owner, 2026-10-20)
// - Contact details and shipments only (C-9)
// =========================================================
public sealed class GetOperationsCounterCustomerUseCase
{
    private readonly OperationsCounterDesk _desk;

    private readonly IOperationsCustomerRepository _customers;

    private readonly OperationsShipmentIntake _intake;

    private readonly IOperationsSupplyRepository _supplies;

    public GetOperationsCounterCustomerUseCase(
        OperationsCounterDesk desk,
        IOperationsCustomerRepository customers,
        OperationsShipmentIntake intake,
        IOperationsSupplyRepository supplies)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
    }

    public async Task<GetOperationsCounterCustomerResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsCounterCustomerRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, IReadOnlyList<OperationsWarehouse> counters, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsCounterCustomerResponse.Failure(refused);
        }

        if (await _customers.GetAsync(request.CustomerId, cancellationToken) is not { } customer)
        {
            return GetOperationsCounterCustomerResponse.Failure(OperationsReceivingErrors.CustomerNotFound);
        }

        // A new shipment leaves from the receiving warehouse the clerk serves at: the customer brings the parcels there, no mailbox address needed.
        IReadOnlyList<OperationsChoice> origins = customer.Status == OperationsCustomerStatus.Active
            ? counters.Where(counter => counter.IsOrigin).Select(counter => new OperationsChoice(counter.Id, counter.Name, counter.Address.City + ", " + counter.Address.CountryCode)).ToList()
            : Array.Empty<OperationsChoice>();

        return GetOperationsCounterCustomerResponse.Success(OperationsCounterCustomer.Of(customer), await _desk.ShipmentsAsync(customer.Id, counters, cancellationToken), origins,
            await _intake.DestinationsAsync(cancellationToken), await _intake.ServicesAsync(customer.Id, cancellationToken), await _intake.InsuranceAsync(cancellationToken),
            await _intake.DeliveryModesAsync(cancellationToken), await _desk.StandingAsync(customer, cancellationToken), await _intake.CurrencyAsync(cancellationToken), _intake.UtcNow,
            (await _supplies.ListCustomerOrdersAsync(customer.Id, 20, cancellationToken)).Where(order => counters.Any(counter => counter.Id == order.WarehouseId)).Take(10).ToList(),
            await _intake.AllowCollectAsync(cancellationToken));
    }
}
