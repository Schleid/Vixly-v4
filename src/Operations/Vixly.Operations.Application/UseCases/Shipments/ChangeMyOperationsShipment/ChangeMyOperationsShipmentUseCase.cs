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

namespace Vixly.Operations.Application.UseCases.Shipments.ChangeMyOperationsShipment;

// =========================================================
// FILE: ChangeMyOperationsShipmentUseCase.cs
//
// PURPOSE:
// A customer changes where an open shipment goes, who receives it or its options (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class ChangeMyOperationsShipmentUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly OperationsShipmentIntake _intake;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly Vixly.Operations.Application.Insurance.OperationsCertificates _certificates;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsShipmentBooking _booking;

    private readonly Vixly.Operations.Application.Inspection.OperationsInspectionSelection _selection;

    public ChangeMyOperationsShipmentUseCase(
        OperationsCustomerAccounts accounts,
        OperationsShipmentIntake intake,
        IOperationsReceivingRepository receiving,
        IOperationsUnitOfWork unitOfWork,
        IOperationsTrackingRepository tracking,
        Vixly.Operations.Application.Inspection.OperationsInspectionSelection selection,
        Vixly.Operations.Application.Insurance.OperationsCertificates certificates,
        OperationsShipmentBooking booking)
    {
        _booking = booking ?? throw new ArgumentNullException(nameof(booking));
        _certificates = certificates ?? throw new ArgumentNullException(nameof(certificates));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _intake = intake ?? throw new ArgumentNullException(nameof(intake));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<ChangeMyOperationsShipmentResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ChangeMyOperationsShipmentRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return ChangeMyOperationsShipmentResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        // Only the customer's own shipment; another customer's answers "not found" (S-3).
        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment || shipment.CustomerId != signedIn.Customer.Id)
        {
            return ChangeMyOperationsShipmentResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        // Slice 4D (review): the waiting shipment is never turned into one to send; its packages are put into the customer's shipments.
        if (shipment.IsWaiting)
        {
            return ChangeMyOperationsShipmentResponse.Failure(OperationsShipmentErrors.Waiting);
        }

        ArgumentNullException.ThrowIfNull(request.Instructions);
        (OperationsShipmentInstructions? instructions, Error? invalid) = await _intake.InstructionsAsync(request.Instructions, cancellationToken, signedIn.Customer.Id);

        if (instructions is null)
        {
            return ChangeMyOperationsShipmentResponse.Failure(invalid!);
        }

        try
        {
            shipment.ChangeInstructions(instructions, _accounts.UtcNow);

            // Slice 5C: the customer corrects the count (4 cartons came, not 3); never below the parcels already in it (C-1).
            if (request.Instructions.ParcelCount is { } count)
            {
                shipment.DeclareParcelCount(count, Vixly.Operations.Application.Shipments.OperationsShipmentBooking.Counted(
                    await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken)).Count, _accounts.UtcNow);
            }
        }
        catch (Vixly.Shared.Kernel.Domain.DomainException exception)
        {
            return ChangeMyOperationsShipmentResponse.Failure(exception.Error);
        }

        // A new destination: the company's rules look at the parcels already checked at the station (Slice 3C, Q-C1).
        foreach (Guid parcelId in (await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken)).Select(item => item.Id).ToList())
        {
            if (await _tracking.GetParcelAsync(parcelId, cancellationToken) is { } parcel)
            {
                // Slice 4D step 2 (review): its shipping label shows the old instructions; it goes back to the labels to print.
                parcel.ShippingLabelOutdated(_accounts.UtcNow);
                await _selection.RecheckAsync(parcel, shipment, _accounts.UtcNow, cancellationToken);
            }
        }

        // Slice 5B: the insurance certificate follows what the customer chose.
        await _certificates.SyncAsync(shipment, _accounts.UtcNow, cancellationToken);

        // Slice 5C: with the right count, every parcel may now be in: the shipment becomes final (C-2, C-4).
        await _booking.TryBookAsync(shipment, OperationsTrackingActor.Customer, null, null, OperationsTrackingSource.Customer, _accounts.UtcNow, cancellationToken);

        // Changed from two devices at once: the row version keeps one.
        return await _unitOfWork.SaveChangesAsync(cancellationToken)
            ? ChangeMyOperationsShipmentResponse.Success(shipment.Id)
            : ChangeMyOperationsShipmentResponse.Failure(Vixly.Operations.Domain.Common.OperationsCommonErrors.ConcurrencyConflict);
    }
}
