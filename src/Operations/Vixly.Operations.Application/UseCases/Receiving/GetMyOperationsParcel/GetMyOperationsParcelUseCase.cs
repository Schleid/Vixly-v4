using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetMyOperationsParcel;

// =========================================================
// FILE: GetMyOperationsParcelUseCase.cs
//
// PURPOSE:
// One of a customer's own packages: history and photos (Slice 3B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Customer history: customer events only, corrected, no actor or note (ADR-0021 decision 5)
// =========================================================
public sealed class GetMyOperationsParcelUseCase
{
    private readonly OperationsCustomerAccounts _accounts;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsInspectionRepository _inspections;

    private readonly IOperationsHoldRepository _holds;

    public GetMyOperationsParcelUseCase(
        OperationsCustomerAccounts accounts,
        IOperationsReceivingRepository receiving,
        IOperationsTrackingRepository tracking,
        IOperationsLocationRepository locations,
        IOperationsSettingsRepository settings,
        IOperationsInspectionRepository inspections,
        IOperationsHoldRepository holds)
    {
        _holds = holds ?? throw new ArgumentNullException(nameof(holds));
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _accounts = accounts ?? throw new ArgumentNullException(nameof(accounts));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<GetMyOperationsParcelResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetMyOperationsParcelRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _accounts.SignedInAsync(request.SessionToken, cancellationToken) is not { } signedIn)
        {
            return GetMyOperationsParcelResponse.Failure(OperationsCustomerAuthErrors.SessionRequired);
        }

        if (await _tracking.GetParcelAsync(request.ParcelId, cancellationToken) is not { } parcel || parcel.CustomerId != signedIn.Customer.Id)
        {
            return GetMyOperationsParcelResponse.Failure(OperationsReceivingErrors.ParcelNotFound);
        }

        string? warehouse = parcel.WarehouseId is { } warehouseId ? (await _locations.GetWarehouseAsync(warehouseId, cancellationToken))?.Name : null;
        IReadOnlyList<OperationsParcelPhotoInfo> photos = (await _receiving.ListPhotosAsync(parcel.Id, cancellationToken))
            .OrderBy(photo => photo.CreatedUtc).Select(photo => new OperationsParcelPhotoInfo(photo.Id, photo.Kind, photo.CreatedUtc)).ToList();
        Vixly.Operations.Domain.Settings.OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);

        // Only while the inspection's hold is still open: once released or answered, nothing is asked any more.
        Vixly.Operations.Domain.Inspection.OperationsInspection? inspection = (await _inspections.ListLatestAsync(new[] { parcel.Id }, cancellationToken)).FirstOrDefault();
        bool asks = inspection is { CustomerAction: not null }
            && ((inspection.HoldReason != OperationsHoldReasons.None && parcel.HoldReasons.HasFlag(inspection.HoldReason))
                || (inspection.Result is "refused_returned" or "refused_disposed" && parcel.HoldReasons.HasFlag(OperationsHoldReasons.Refused)));

        // Slice 4D step 3: the storage fee so far, while it waits here.
        decimal storage = parcel.OwesStorage
            ? Vixly.Operations.Domain.Holds.OperationsStorageFees.Accrued(parcel.CreatedUtc, _accounts.UtcNow,
                parcel.StorageFreeDays ?? settings?.StorageFreeDays ?? Vixly.Operations.Domain.Holds.OperationsHoldLimits.DefaultUnclaimedDays,
                parcel.StorageFreeDays is not null ? parcel.StorageFeePerDay : settings?.StorageFeePerDay)
            : 0m;

        return GetMyOperationsParcelResponse.Success(
            OperationsCustomerParcel.Of(parcel, warehouse),
            OperationsTrackingViews.ForCustomer(await _tracking.ListForSubjectAsync(OperationsTrackingSubject.Parcel, parcel.Id, cancellationToken)),
            photos,
            (await _receiving.FindReceiptAsync(parcel.Id, cancellationToken))?.Number,
            settings?.WeightUnit ?? OperationsWeightUnit.Kilogram,
            settings?.DimensionUnit ?? OperationsDimensionUnit.Centimetre,
            asks ? inspection!.CustomerAction : null,
            asks ? inspection!.Fee : null,
            asks ? inspection!.FeeCurrency : null,
            Enum.GetValues<OperationsHoldReasons>().Where(reason => reason != OperationsHoldReasons.None && parcel.HoldReasons.HasFlag(reason)
                && Vixly.Operations.Domain.Holds.OperationsHoldAnswer.KindFor(reason) is not null).ToList(),
            (await _holds.ListLatestAnswersAsync(new[] { parcel.Id }, cancellationToken)).Select(Vixly.Operations.Application.Holds.OperationsHoldAnswerView.Of).ToList())
            .WithStorageFee(storage, settings?.CurrencyCode ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.DefaultCurrency);
    }
}
