using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Insurance;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.ChangeOperationsCounterInsurance;

// =========================================================
// FILE: ChangeOperationsCounterInsuranceUseCase.cs
//
// PURPOSE:
// At the counter, the customer takes another insurance product, or none, for an open shipment (Slice 5D, D-6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.counter.serve at the shipment warehouse; only while the company allows staff to change insurance
// - The customer accepts the terms; the clerk is recorded as the one who recorded it, never as the one who accepted
// =========================================================
public sealed class ChangeOperationsCounterInsuranceUseCase
{
    private readonly OperationsCounterDesk _desk;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsShipmentIntake _intake;

    private readonly OperationsCertificates _certificates;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly OperationsAuditTrail _audit;

    private readonly TimeProvider _timeProvider;

    public ChangeOperationsCounterInsuranceUseCase(
        OperationsCounterDesk desk,
        IOperationsReceivingRepository receiving,
        IOperationsCustomerRepository customers,
        IOperationsSettingsRepository settings,
        OperationsShipmentIntake intake,
        OperationsCertificates certificates,
        IOperationsUnitOfWork unitOfWork,
        OperationsAuditTrail audit,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
        _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<ChangeOperationsCounterInsuranceResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeOperationsCounterInsuranceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> counters, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return ChangeOperationsCounterInsuranceResponse.Failure(refused);
        }

        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment)
        {
            return ChangeOperationsCounterInsuranceResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        // The company's policy decides whether staff may change it (D-6).
        if ((await _settings.FindAsync(cancellationToken))?.StaffMayChangeInsurance == false)
        {
            return ChangeOperationsCounterInsuranceResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (!counters.Any(counter => counter.Id == shipment.OriginWarehouseId))
        {
            return ChangeOperationsCounterInsuranceResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (shipment.Status != OperationsShipmentStatus.Open || shipment.IsWaiting)
        {
            return ChangeOperationsCounterInsuranceResponse.Failure(shipment.IsBooked ? OperationsShipmentErrors.Booked : OperationsShipmentErrors.Waiting);
        }

        if (await _customers.GetAsync(shipment.CustomerId, cancellationToken) is not { Status: OperationsCustomerStatus.Active })
        {
            return ChangeOperationsCounterInsuranceResponse.Failure(OperationsCustomerErrors.NotActive);
        }

        // One of the products in use for the shipment's service.
        if (request.InsurancePlanId is { } planId
            && !OperationsShipmentIntake.ProductsFor(await _intake.InsuranceAsync(cancellationToken), shipment.ServiceCode).Any(plan => plan.Id == planId))
        {
            return ChangeOperationsCounterInsuranceResponse.Failure(OperationsShipmentErrors.InsuranceChoiceRequired);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        Guid? before = shipment.Options.HasFlag(OperationsShipmentOptions.Insured) ? shipment.InsurancePlanId : null;

        try
        {
            shipment.ChangeInsurance(request.InsurancePlanId, request.Accepted, access!.User.Id, utcNow);
        }
        catch (DomainException exception)
        {
            return ChangeOperationsCounterInsuranceResponse.Failure(exception.Error);
        }

        // Who changed it and from what, including a removal (review of 5D): the shipment keeps only who recorded an acceptance.
        _audit.Record(access.User.Id, "ops.shipments.insurance_changed", nameof(OperationsShipment), shipment.Id,
            before: new { plan = before }, after: new { plan = request.InsurancePlanId, number = shipment.Number });

        // The certificate follows the choice (Slice 5B).
        await _certificates.SyncAsync(shipment, utcNow, cancellationToken);

        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeOperationsCounterInsuranceResponse.Success()
            : ChangeOperationsCounterInsuranceResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
