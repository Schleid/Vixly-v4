using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.EstimateMyOperationsShipment;

// =========================================================
// FILE: EstimateMyOperationsShipmentUseCase.cs
//
// PURPOSE:
// The shipment estimate the customer sees on the portal while building or changing a shipment, and on their shipment (Slice 5D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - The customer's own shipment only; another customer's gives not found (S-3)
// - Read only; always an estimate, no taxes, no delivery fees (owner, 2026-10-20)
// =========================================================
public sealed class EstimateMyOperationsShipmentUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsShipmentIntake _intake;

    private readonly OperationsShipmentEstimator _estimator;

    private readonly IOperationsSupplyRepository _supplies;

    public EstimateMyOperationsShipmentUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsReceivingRepository receiving,
        IOperationsSettingsRepository settings,
        OperationsShipmentIntake intake,
        OperationsShipmentEstimator estimator,
        IOperationsSupplyRepository supplies)
    {
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
        _supplies = supplies ?? throw new ArgumentNullException(nameof(supplies));
    }

    public async Task<EstimateMyOperationsShipmentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        EstimateMyOperationsShipmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return EstimateMyOperationsShipmentResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        OperationsWeightUnit weight = settings?.WeightUnit ?? OperationsWeightUnit.Kilogram;
        OperationsDimensionUnit length = settings?.DimensionUnit ?? OperationsDimensionUnit.Centimetre;
        OperationsShipmentDraft? draft = request.Draft;
        IReadOnlyList<(string?, OperationsRatingPiece)> parcels = Array.Empty<(string?, OperationsRatingPiece)>();
        IReadOnlyList<OperationsEstimateSupply> supplies = Array.Empty<OperationsEstimateSupply>();
        Guid? originId = request.OriginWarehouseId;

        // Their own shipment only (S-3): its parcels, and its origin.
        if (request.ShipmentId is { } shipmentId)
        {
            if (await _receiving.GetShipmentAsync(shipmentId, cancellationToken) is not { } shipment || shipment.CustomerId != signedIn.Customer.Id)
            {
                return EstimateMyOperationsShipmentResponse.Failure(OperationsShipmentErrors.NotFound);
            }

            originId = shipment.OriginWarehouseId;

            // Slice 5E (S-16): the supplies the counter added to it, in the customer's language.
            if ((await _supplies.ListShipmentSalesAsync(new[] { shipment.Id }, cancellationToken)).FirstOrDefault() is { } sale)
            {
                supplies = OperationsEstimateSupply.Of(sale, await _supplies.ListAsync(cancellationToken), signedIn.Customer.PreferredLanguage.Value);
            }
            parcels = OperationsShipmentBooking.Counted(await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken)).Where(parcel => parcel.WeightKg is > 0m)
                .Select(parcel => ((string?)parcel.Number, new OperationsRatingPiece(parcel.WeightKg!.Value, parcel.LengthCm ?? 0m, parcel.WidthCm ?? 0m, parcel.HeightCm ?? 0m,
                    parcel.DeclaredValue ?? 0m, null, parcel.DeclaredContents))).ToList();
        }

        // Not enough to price yet, or a service or destination the company does not offer this customer: the panel says what is missing.
        if (draft is null || string.IsNullOrWhiteSpace(draft.Service) || draft.DestinationId is not { } destination
            || !(await _intake.ServicesAsync(signedIn.Customer.Id, cancellationToken)).Contains(draft.Service.Trim(), StringComparer.Ordinal)
            || !(await _intake.DestinationsAsync(cancellationToken)).Any(warehouse => warehouse.Id == destination))
        {
            return EstimateMyOperationsShipmentResponse.Success(null, weight, length);
        }

        IReadOnlyList<Vixly.Operations.Domain.Charges.OperationsInsurancePlan> products = OperationsShipmentIntake.ProductsFor(await _intake.InsuranceAsync(cancellationToken), draft.Service);
        decimal? expected = request.ExpectedValue is >= 0m and <= 1_000_000m ? request.ExpectedValue : null;
        Guid? product = draft.Insured ? draft.InsurancePlanId is { } chosen && products.Any(plan => plan.Id == chosen) ? chosen : products.Count == 1 ? products[0].Id : null : null;
        OperationsEstimate estimate = await _estimator.EstimateAsync(signedIn.Customer.TenantId, new OperationsEstimateInput(signedIn.Customer.Id, draft.Service.Trim(), originId, destination,
            draft.Delivery == "home" ? OperationsDeliveryModes.HomeDelivery : OperationsDeliveryModes.Pickup, draft.Consolidate, product, parcels, expected, supplies), products, cancellationToken);
        return EstimateMyOperationsShipmentResponse.Success(estimate, weight, length);
    }
}
