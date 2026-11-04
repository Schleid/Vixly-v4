using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.EstimateOperationsCounterShipment;

// =========================================================
// FILE: EstimateOperationsCounterShipmentUseCase.cs
//
// PURPOSE:
// The shipment estimate while the clerk fills in a new shipment, before any parcel (Slice 5D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.counter.serve; read only, nothing is saved
// - Always an estimate: no taxes, no delivery fees (owner, 2026-10-20)
// =========================================================
public sealed class EstimateOperationsCounterShipmentUseCase
{
    private readonly OperationsCounterDesk _desk;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsShipmentIntake _intake;

    private readonly OperationsShipmentEstimator _estimator;

    public EstimateOperationsCounterShipmentUseCase(
        OperationsCounterDesk desk,
        IOperationsCustomerRepository customers,
        IOperationsSettingsRepository settings,
        OperationsShipmentIntake intake,
        OperationsShipmentEstimator estimator)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
    }

    public async Task<EstimateOperationsCounterShipmentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        EstimateOperationsCounterShipmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, IReadOnlyList<OperationsWarehouse> _, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return EstimateOperationsCounterShipmentResponse.Failure(refused);
        }

        if (await _customers.GetAsync(request.CustomerId, cancellationToken) is not { } customer)
        {
            return EstimateOperationsCounterShipmentResponse.Failure(OperationsReceivingErrors.CustomerNotFound);
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        OperationsWeightUnit weight = settings?.WeightUnit ?? OperationsWeightUnit.Kilogram;
        OperationsDimensionUnit length = settings?.DimensionUnit ?? OperationsDimensionUnit.Centimetre;

        // Not enough to price yet: the panel asks for the destination and the service.
        if (string.IsNullOrWhiteSpace(request.ServiceCode) || request.DestinationWarehouseId is null)
        {
            return EstimateOperationsCounterShipmentResponse.Success(null, weight, length);
        }

        decimal? expected = request.ExpectedValue is >= 0m and <= 1_000_000m ? request.ExpectedValue : null;
        IReadOnlyList<Vixly.Operations.Domain.Charges.OperationsInsurancePlan> products = OperationsShipmentIntake.ProductsFor(await _intake.InsuranceAsync(cancellationToken), request.ServiceCode);
        OperationsEstimate estimate = await _estimator.EstimateAsync(context.RequiredTenantId, new OperationsEstimateInput(customer.Id, request.ServiceCode.Trim(), request.OriginWarehouseId,
            request.DestinationWarehouseId, request.Delivery == "home" ? OperationsDeliveryModes.HomeDelivery : OperationsDeliveryModes.Pickup, request.Consolidate,
            products.Any(product => product.Id == request.InsurancePlanId) ? request.InsurancePlanId : null, Array.Empty<(string?, OperationsRatingPiece)>(), expected), products,
            cancellationToken);
        return EstimateOperationsCounterShipmentResponse.Success(estimate, weight, length);
    }
}
