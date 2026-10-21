using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.AddOperationsCounterParcel;

// =========================================================
// FILE: AddOperationsCounterParcelUseCase.cs
//
// PURPOSE:
// Enters a parcel at the counter: weighed, measured and described in front of the customer; the last one makes the shipment final (Slice 5C, C-2 and C-7).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Needs ops.counter.serve at the shipment's origin warehouse
// - Weight and size in the company's units; the value in its currency; no photo at the counter, the customer is present (C-7, to confirm)
// - The same parcel sent twice is entered once (ADR-0019)
// =========================================================
public sealed class AddOperationsCounterParcelUseCase
{
    private readonly OperationsCounterDesk _desk;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsNumbering _numbering;

    private readonly OperationsTracker _tracker;

    private readonly OperationsInspectionSelection _selection;

    private readonly OperationsShipmentBooking _booking;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AddOperationsCounterParcelUseCase(
        OperationsCounterDesk desk,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        IOperationsSettingsRepository settings,
        OperationsNumbering numbering,
        OperationsTracker tracker,
        OperationsInspectionSelection selection,
        OperationsShipmentBooking booking,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider,
        IOperationsCustomerRepository customers)
    {
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _tracker = tracker ?? throw new ArgumentNullException(nameof(tracker));
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        _booking = booking ?? throw new ArgumentNullException(nameof(booking));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AddOperationsCounterParcelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AddOperationsCounterParcelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> counters, Error? refused) = await _desk.OpenAsync(context, cancellationToken);

        if (refused is not null)
        {
            return AddOperationsCounterParcelResponse.Failure(refused);
        }

        if (!OperationsCommandKey.IsValid(request.CommandKey))
        {
            return AddOperationsCounterParcelResponse.Failure(OperationsTrackingErrors.IdempotencyKeyInvalid);
        }

        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment)
        {
            return AddOperationsCounterParcelResponse.Failure(OperationsShipmentErrors.NotFound);
        }

        string key = "ctr:" + request.CommandKey;

        // The same parcel sent twice (a double click): the one it entered.
        if (await _tracking.FindStoredByKeyAsync(key + ":received", cancellationToken) is { } earlier)
        {
            return AddOperationsCounterParcelResponse.Success(earlier.SubjectId, shipment.IsBooked);
        }

        if (counters.FirstOrDefault(counter => counter.Id == shipment.OriginWarehouseId) is not { } warehouse)
        {
            return AddOperationsCounterParcelResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        if (shipment.Status != OperationsShipmentStatus.Open)
        {
            return AddOperationsCounterParcelResponse.Failure(shipment.IsBooked ? OperationsShipmentErrors.Booked : OperationsReceivingErrors.ShipmentNotOpen);
        }

        // Never for a suspended or closed account (review): the same rule as a new shipment.
        if (await _customers.GetAsync(shipment.CustomerId, cancellationToken) is not { Status: Vixly.Operations.Domain.Customers.OperationsCustomerStatus.Active })
        {
            return AddOperationsCounterParcelResponse.Failure(Vixly.Operations.Domain.Customers.OperationsCustomerErrors.NotActive);
        }

        // Its instructions and count come first: where it goes, for whom, and how many parcels.
        if (!shipment.HasInstructions)
        {
            return AddOperationsCounterParcelResponse.Failure(OperationsShipmentErrors.Waiting);
        }

        if (shipment.ParcelCount is not { } count)
        {
            return AddOperationsCounterParcelResponse.Failure(OperationsShipmentErrors.ParcelCountInvalid);
        }

        // Exactly that many: more boxes means a new shipment (owner, 2026-10-20).
        if (OperationsShipmentBooking.Counted(await _receiving.ListShipmentParcelsAsync(shipment.Id, cancellationToken)).Count >= count)
        {
            return AddOperationsCounterParcelResponse.Failure(OperationsShipmentErrors.ParcelCountReached);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
        OperationsParcel parcel;

        try
        {
            if (request.Weight is not { } weight || request.Length is not { } length || request.Width is not { } width || request.Height is not { } height)
            {
                return AddOperationsCounterParcelResponse.Failure(OperationsReceivingErrors.MeasurementsInvalid);
            }

            OperationsMeasurements measurements = OperationsMeasurements.Create(
                OperationsUnits.ToKilograms(weight, settings.WeightUnit),
                OperationsUnits.ToCentimetres(length, settings.DimensionUnit),
                OperationsUnits.ToCentimetres(width, settings.DimensionUnit),
                OperationsUnits.ToCentimetres(height, settings.DimensionUnit));
            parcel = OperationsParcel.DropOff(context.RequiredTenantId, await _numbering.NextAsync(OperationsNumberKind.Parcel, cancellationToken), shipment, measurements,
                request.Contents, request.Value, settings.CurrencyCode, utcNow);
        }
        catch (DomainException exception)
        {
            return AddOperationsCounterParcelResponse.Failure(exception.Error);
        }

        parcel.TakeStorageTerms(settings.StorageFreeDays, settings.StorageFeePerDay);
        _tracking.Add(parcel);
        shipment.TakePackage(utcNow);

        // Received at the counter, then measured: the same history as the dock and the station, from the counter (C-7).
        foreach (OperationsTrackingDraft step in new[]
        {
            Step(key + ":received", parcel, OperationsTrackingCodes.ReceivedAtOrigin, access!, warehouse, utcNow, ("warehouse", warehouse.Code)),
            Step(key + ":checked", parcel, OperationsTrackingCodes.Checked, access!, warehouse, utcNow,
                ("weight", parcel.WeightKg!.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)),
                ("size", string.Join(" x ", new[] { parcel.LengthCm!.Value, parcel.WidthCm!.Value, parcel.HeightCm!.Value }.Select(side => side.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture)))))
        })
        {
            if ((await _tracker.RecordAsync(step, cancellationToken)).Error is { } stepRefused)
            {
                return AddOperationsCounterParcelResponse.Failure(stepRefused);
            }
        }

        // The company's inspection rules and random share apply as at the station (C-7).
        if (await _selection.ChooseAsync(parcel, true, null, cancellationToken) is { } choice)
        {
            await _selection.OpenAsync(parcel, choice, access!.User.Id, key + ":inspection", OperationsTrackingActor.Staff, OperationsTrackingSource.Counter, warehouse.Code, utcNow,
                cancellationToken);
        }

        bool booked = await _booking.TryBookAsync(shipment, OperationsTrackingActor.Staff, access!.User.Id, warehouse.Code, OperationsTrackingSource.Counter, utcNow, cancellationToken);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return AddOperationsCounterParcelResponse.Success(parcel.Id, booked);
        }

        _unitOfWork.DiscardChanges();
        return await _tracking.FindStoredByKeyAsync(key + ":received", cancellationToken) is { } raced
            ? AddOperationsCounterParcelResponse.Success(raced.SubjectId,
                await _tracking.FindStoredByKeyAsync("book:" + shipment.Id.ToString("N") + ":" + raced.SubjectId.ToString("N"), cancellationToken) is not null)
            : AddOperationsCounterParcelResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }

    private static OperationsTrackingDraft Step(string key, OperationsParcel parcel, string code, OperationsAccessView access, OperationsWarehouse warehouse, DateTimeOffset utcNow,
        params (string Name, string Value)[] arguments) =>
        new(OperationsTrackingSubject.Parcel, parcel.Id, code, arguments.ToDictionary(item => item.Name, item => item.Value, StringComparer.Ordinal), utcNow, key,
            OperationsTrackingActor.Staff, OperationsTrackingSource.Counter)
        {
            ActorUserId = access.User.Id,
            LocationCode = warehouse.Code
        };
}
