using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.UseCases.Site.EstimateOperationsPublicShipment;

// =========================================================
// FILE: EstimateOperationsPublicShipmentUseCase.cs
//
// PURPOSE:
// A visitor's estimate on the company's website (owner, 2026-10-30): a weight to a pickup destination with a published service.
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Anonymous: the regular published prices only, never a customer's; always shown as an estimate (no customs, taxes or insurance)
// =========================================================
public sealed class EstimateOperationsPublicShipmentUseCase
{
    // A parcel a visitor may price on the website, in the company's unit.
    public const decimal MaxWeight = 1000m;

    private readonly OperationsPricing _pricing;

    private readonly OperationsShipmentIntake _intake;

    private readonly OperationsShipmentEstimator _estimator;

    private readonly IOperationsSettingsRepository _settings;

    public EstimateOperationsPublicShipmentUseCase(OperationsPricing pricing, OperationsShipmentIntake intake, OperationsShipmentEstimator estimator, IOperationsSettingsRepository settings)
    {
        _pricing = pricing ?? throw new ArgumentNullException(nameof(pricing));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
        _estimator = estimator ?? throw new ArgumentNullException(nameof(estimator));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<EstimateOperationsPublicShipmentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        EstimateOperationsPublicShipmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        // Only while the company publishes its prices, and only with a service it publishes.
        if (!await _pricing.PublicShownAsync(cancellationToken))
        {
            return EstimateOperationsPublicShipmentResponse.Failure(OperationsCommonErrors.NotFound);
        }

        if (request.Weight is not { } weight || weight <= 0m || weight > MaxWeight || string.IsNullOrWhiteSpace(request.Service)
            || request.DestinationId is not { } destinationId || !(await _intake.DestinationsAsync(cancellationToken)).Any(warehouse => warehouse.Id == destinationId))
        {
            return EstimateOperationsPublicShipmentResponse.Success(null);
        }

        OperationsPricingView? pricing = await _pricing.PublicAsync(context.RequiredTenantId, cancellationToken);
        string service = request.Service.Trim();

        if (pricing is null || !pricing.Lists.Any(list => string.Equals(list.ServiceCode, service, StringComparison.Ordinal)))
        {
            return EstimateOperationsPublicShipmentResponse.Success(null);
        }

        // The weight is in the company's unit; the engine rates kilograms and rounds the billable weight up in the company's unit.
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        decimal kilograms = settings?.WeightUnit == OperationsWeightUnit.Pound ? weight * 0.45359237m : weight;
        OperationsRatingPiece piece = new(kilograms, 0m, 0m, 0m, 0m, null, null);
        OperationsEstimate estimate = await _estimator.EstimateAsync(context.RequiredTenantId,
            new OperationsEstimateInput(null, service, null, destinationId, OperationsDeliveryModes.Pickup, false, null, new[] { ((string?)null, piece) }),
            Array.Empty<Vixly.Operations.Domain.Charges.OperationsInsurancePlan>(), cancellationToken);
        return EstimateOperationsPublicShipmentResponse.Success(estimate.Priced ? estimate : null);
    }
}
