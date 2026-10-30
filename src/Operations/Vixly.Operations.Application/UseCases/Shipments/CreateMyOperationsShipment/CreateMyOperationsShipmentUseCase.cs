using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.CreateMyOperationsShipment;

// =========================================================
// FILE: CreateMyOperationsShipmentUseCase.cs
//
// PURPOSE:
// A customer creates a shipment: where it goes, who receives it, the service and options; with a new package, packages it announced, or none (Slice 4D).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class CreateMyOperationsShipmentUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsShipmentIntake _intake;

    private readonly OperationsPreAlertIntake _preAlerts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly Vixly.Operations.Application.Insurance.OperationsCertificates _certificates;

    private readonly OperationsPackageMover _mover;

    private readonly Vixly.Operations.Application.Settings.OperationsNumbering _numbering;

    private readonly OperationsShipmentBooking _booking;

    public CreateMyOperationsShipmentUseCase(
        OperationsCustomerAccounts accounts,
        OperationsShipmentIntake intake,
        OperationsPreAlertIntake preAlerts,
        IOperationsReceivingRepository receiving,
        IOperationsUnitOfWork unitOfWork,
        OperationsPackageMover mover,
        Vixly.Operations.Application.Settings.OperationsNumbering numbering,
        Vixly.Operations.Application.Insurance.OperationsCertificates certificates,
        OperationsShipmentBooking booking)
    {
        _booking = booking ?? throw new ArgumentNullException(nameof(booking));
        _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _mover = mover ?? throw new ArgumentNullException(nameof(mover));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
        _preAlerts = preAlerts ?? throw new ArgumentNullException(nameof(preAlerts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<CreateMyOperationsShipmentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CreateMyOperationsShipmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return CreateMyOperationsShipmentResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        ArgumentNullException.ThrowIfNull(request.Instructions);

        // Slice 5C (C-1, review): every new shipment says how many parcels travel in it.
        if (request.Instructions.ParcelCount is not { } count)
        {
            return CreateMyOperationsShipmentResponse.Failure(OperationsShipmentErrors.ParcelCountInvalid);
        }

        string? requestKey = Vixly.Operations.Application.Receiving.OperationsCommandKey.IsValid(request.RequestKey) ? request.RequestKey : null;

        // The same form sent twice (a double click past the browser guard): the shipment it created.
        if (requestKey is not null && await _receiving.FindShipmentByKeyAsync(signedIn.Customer.Id, requestKey, cancellationToken) is { } earlier)
        {
            return CreateMyOperationsShipmentResponse.Success(earlier.Id, null, null);
        }

        if (!(await _intake.OriginsAsync(signedIn.Customer, cancellationToken)).Any(origin => origin.Id == request.OriginWarehouseId))
        {
            return CreateMyOperationsShipmentResponse.Failure(OperationsShipmentErrors.OriginInvalid);
        }

        (OperationsShipmentInstructions? instructions, Error? invalid) = await _intake.InstructionsAsync(request.Instructions, cancellationToken, signedIn.Customer.Id);

        if (instructions is null)
        {
            return CreateMyOperationsShipmentResponse.Failure(invalid!);
        }

        DateTimeOffset utcNow = _accounts.UtcNow;
        OperationsShipment shipment = OperationsShipment.Request(context.RequiredTenantId, signedIn.Customer.Id, request.OriginWarehouseId, instructions, utcNow);
        shipment.Keyed(requestKey);

        // Slice 4D step 2: its number, on every shipping label of its parcels.
        shipment.Numbered(await _numbering.NextAsync(OperationsNumberKind.Shipment, cancellationToken));
        _receiving.Add(shipment);
        OperationsPreAlert? preAlert = null;
        OperationsParcel? parcel = null;

        if (request.Package is { } package)
        {
            (preAlert, parcel, Error? refused) = await _preAlerts.AnnounceAsync(shipment, package, utcNow, cancellationToken);

            if (preAlert is null)
            {
                return CreateMyOperationsShipmentResponse.Failure(refused!);
            }
        }

        // Slice 4D: the packages the customer chose go into it now.
        foreach ((Guid? preAlertId, Guid? parcelId) in request.PreAlertIds.Distinct().Select(id => ((Guid?)id, (Guid?)null))
            .Concat(request.ParcelIds.Distinct().Select(id => ((Guid?)null, (Guid?)id))))
        {
            if (await _mover.PlaceForCustomerAsync(signedIn.Customer.Id, preAlertId, parcelId, shipment, utcNow, cancellationToken) is { } notPlaced)
            {
                return CreateMyOperationsShipmentResponse.Failure(notPlaced);
            }
        }

        // Slice 5C: how many parcels the customer says travel in it; never fewer than the parcels now in it, received pre-alerts included (C-1, review).
        try
        {
            shipment.DeclareParcelCount(count, OperationsShipmentBooking.Counted(await _receiving.ListShipmentParcelsForUpdateAsync(shipment.Id, cancellationToken)).Count, utcNow);
        }
        catch (Vixly.Shared.Kernel.Domain.DomainException exception)
        {
            return CreateMyOperationsShipmentResponse.Failure(exception.Error);
        }

        // Slice 5B: the insurance certificate follows what the customer chose.
        await _certificates.SyncAsync(shipment, utcNow, cancellationToken);

        // Slice 5C (review): the parcels already here may be all of them; then it is final at once (C-2).
        await _booking.TryBookAsync(shipment, OperationsTrackingActor.Customer, null, null, OperationsTrackingSource.Customer, utcNow, cancellationToken);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CreateMyOperationsShipmentResponse.Success(shipment.Id, preAlert?.Id, parcel?.Id);
        }

        // The same form saved first at the same moment wins; otherwise two announcements of one number at once, the unique index refusing the second.
        _unitOfWork.DiscardChanges();
        return requestKey is not null && await _receiving.FindShipmentByKeyAsync(signedIn.Customer.Id, requestKey, cancellationToken) is { } raced
            ? CreateMyOperationsShipmentResponse.Success(raced.Id, null, null)
            : CreateMyOperationsShipmentResponse.Failure(OperationsReceivingErrors.PreAlertDuplicate);
    }
}
