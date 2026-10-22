using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Printing;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcel;

// =========================================================
// FILE: GetOperationsParcelUseCase.cs
//
// PURPOSE:
// One parcel for staff: details, customer, shipment, history, photos and receipt (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Staff history: every event, voided ones marked (ADR-0021 decision 5)
// - The inspection station shows the customer and the other parcels of the shipment (owner, 2026-10-18)
// =========================================================
public sealed class GetOperationsParcelUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsPrintingRepository _printing;

    private readonly OperationsParcelReader _reader;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsInspectionRepository _inspections;

    private readonly TimeProvider _timeProvider;

    public GetOperationsParcelUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        IOperationsCustomerRepository customers,
        IOperationsPrintingRepository printing,
        OperationsParcelReader reader,
        IOperationsSettingsRepository settings,
        IOperationsInspectionRepository inspections,
        TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _printing = printing ?? throw new ArgumentNullException(nameof(printing));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<GetOperationsParcelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsParcelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsParcelResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        // Another warehouse's parcel and a missing parcel give the same answer (S-3).
        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { WarehouseId: { } warehouseId } parcel
            || await _scope.CoveredAsync(access, OperationsPermissions.ParcelsView, warehouseId, cancellationToken) is not { } warehouse)
        {
            return GetOperationsParcelResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        IReadOnlyList<OperationsTrackingLine> history = OperationsTrackingViews.ForStaff(await _tracking.ListForSubjectAsync(OperationsTrackingSubject.Parcel, parcel.Id, cancellationToken));
        IReadOnlyList<OperationsParcelPhotoInfo> photos = (await _receiving.ListPhotosAsync(parcel.Id, cancellationToken))
            .OrderBy(photo => photo.CreatedUtc).Select(photo => new OperationsParcelPhotoInfo(photo.Id, photo.Kind, photo.CreatedUtc)).ToList();
        bool canPutAway = access.CoversWarehouse(OperationsPermissions.ReceivingReceive, warehouse.Id, warehouse.BranchId);
        IReadOnlyList<OperationsStorageLocation> locations = canPutAway
            ? (await _receiving.ListLocationsAsync(warehouse.Id, cancellationToken)).Where(location => location.IsActive).ToList()
            : Array.Empty<OperationsStorageLocation>();
        IReadOnlyList<OperationsParcel> others = parcel.ShipmentId is { } shipmentId
            ? (await _receiving.ListShipmentParcelsAsync(shipmentId, cancellationToken)).Where(other => other.Id != parcel.Id).ToList()
            : Array.Empty<OperationsParcel>();
        IReadOnlyList<OperationsParcelSummary> summaries = await _reader.SummariesAsync(new[] { parcel }.Concat(others).ToList(), cancellationToken);
        string? condition = history.LastOrDefault(line => line.Code == OperationsTrackingCodes.ConditionRecorded && !line.Voided)?.Arguments.GetValueOrDefault("condition");
        OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        GetOperationsParcelResponse result = GetOperationsParcelResponse.Success(
            summaries[0],
            history,
            photos,
            (await _receiving.FindReceiptAsync(parcel.Id, cancellationToken))?.Number,
            condition,
            locations,
            !parcel.IsIdentified && access.CoversWarehouse(OperationsPermissions.ParcelsAssign, warehouse.Id, warehouse.BranchId),
            canPutAway,
            parcel.IsAtOrigin && access.CoversWarehouse(OperationsPermissions.InspectionCheck, warehouse.Id, warehouse.BranchId),
            parcel.CustomerId is { } customerId ? (await _customers.GetAsync(customerId, cancellationToken))?.Phone : null,
            summaries.Skip(1).ToList(),
            (await _printing.ListPrintersAsync(warehouse.Id, cancellationToken)).Where(printer => printer.IsActive).ToList(),
            settings?.WeightUnit ?? OperationsWeightUnit.Kilogram,
            settings?.DimensionUnit ?? OperationsDimensionUnit.Centimetre,
            await _reader.ShipmentAsync(parcel.ShipmentId, summaries.Count, cancellationToken),
            (await _inspections.ListLatestAsync(new[] { parcel.Id }, cancellationToken)).FirstOrDefault(),
            parcel.ShipmentId is { } own && await _receiving.GetShipmentAsync(own, cancellationToken) is { } shipment && parcel.IsClearedIn(shipment));

        // Slice 4D step 3: the customer whose mailbox code is on the label (for staff to check), and the storage fee so far.
        Vixly.Operations.Domain.Customers.OperationsCustomer? labelCustomer = !parcel.IsIdentified && parcel.IsAtOrigin && parcel.LabelCode is { } labelCode
            && await _customers.FindByCodeAsync(labelCode, cancellationToken) is { Status: Vixly.Operations.Domain.Customers.OperationsCustomerStatus.Active } found ? found : null;
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        int freeDays = parcel.StorageFreeDays ?? settings?.StorageFreeDays ?? Vixly.Operations.Domain.Holds.OperationsHoldLimits.DefaultUnclaimedDays;
        decimal? perDay = parcel.StorageFreeDays is not null ? parcel.StorageFeePerDay : settings?.StorageFeePerDay;
        bool stored = parcel.OwesStorage;

        return result.WithStorage(labelCustomer,
            stored ? Vixly.Operations.Domain.Holds.OperationsStorageFees.Accrued(parcel.CreatedUtc, utcNow, freeDays, perDay) : 0m,
            stored ? Vixly.Operations.Domain.Holds.OperationsStorageFees.DaysCharged(parcel.CreatedUtc, utcNow, freeDays) : 0,
            settings?.CurrencyCode ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.DefaultCurrency,
            access.Has(OperationsPermissions.CustomersView) || access.IsAdministrator);
    }
}
