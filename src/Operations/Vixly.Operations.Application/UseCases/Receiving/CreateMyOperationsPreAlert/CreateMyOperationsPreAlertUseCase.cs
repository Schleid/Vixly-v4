using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.CreateMyOperationsPreAlert;

// =========================================================
// FILE: CreateMyOperationsPreAlertUseCase.cs
//
// PURPOSE:
// A customer announces a package: on its own for a warehouse, or in one of its shipments (Slice 3B; Slice 4D, owner, 2026-10-19).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Written by the customer only; staff never type it (D-C6)
// - Pre-alerts come first; the customer puts them into shipments later (owner, 2026-10-19)
// =========================================================
public sealed class CreateMyOperationsPreAlertUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly OperationsPreAlertIntake _preAlerts;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly OperationsShipmentIntake _intake;

    public CreateMyOperationsPreAlertUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsReceivingRepository receiving,
        OperationsPreAlertIntake preAlerts,
        IOperationsUnitOfWork unitOfWork,
        OperationsShipmentIntake intake)
    {
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _preAlerts = preAlerts ?? throw new ArgumentNullException(nameof(preAlerts));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CreateMyOperationsPreAlertResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CreateMyOperationsPreAlertRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return CreateMyOperationsPreAlertResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        // Only into the customer's own open shipment with instructions; another customer's answers "not found" (S-3).
        OperationsShipment? shipment = request.ShipmentId is { } shipmentId ? await _receiving.GetShipmentAsync(shipmentId, cancellationToken) : null;

        if (request.ShipmentId is not null && (shipment is null || shipment.CustomerId != signedIn.Customer.Id))
        {
            return CreateMyOperationsPreAlertResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        // The warehouse: the shipment's origin, or one where the customer has a mailbox address (the only one when it is left out).
        IReadOnlyList<OperationsChoice> origins = await _intake.OriginsAsync(signedIn.Customer, cancellationToken);
        Guid? warehouseId = shipment?.OriginWarehouseId ?? request.WarehouseId ?? (origins.Count == 1 ? origins[0].Id : null);

        if (warehouseId is not { } warehouse || (shipment is null && !origins.Any(origin => origin.Id == warehouse)))
        {
            return CreateMyOperationsPreAlertResponse.Failure(OperationsShipmentErrors.OriginInvalid);
        }

        (OperationsPreAlert? preAlert, OperationsParcel? parcel, Error? refused) = await _preAlerts.AnnounceAsync(context.RequiredTenantId, signedIn.Customer.Id, warehouse, shipment,
            new OperationsPackageDraft(request.Carrier, request.TrackingNumber, request.Store, request.Contents, request.DeclaredValue, request.ExpectedDate), _accounts.UtcNow, cancellationToken);

        if (preAlert is null)
        {
            return CreateMyOperationsPreAlertResponse.Failure(refused!);
        }

        // Two announcements of one number at once: the unique index refuses the second.
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? CreateMyOperationsPreAlertResponse.Success(preAlert.Id, parcel?.Id)
            : CreateMyOperationsPreAlertResponse.Failure(OperationsReceivingErrors.PreAlertDuplicate);
    }
}
