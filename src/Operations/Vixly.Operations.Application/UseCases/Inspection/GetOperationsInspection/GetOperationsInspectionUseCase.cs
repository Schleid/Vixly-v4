using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.GetOperationsInspection;

// =========================================================
// FILE: GetOperationsInspectionUseCase.cs
//
// PURPOSE:
// One inspection with its parcel, shipment and the restricted items that apply (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Another warehouse's inspection answers 'not found' (scope by place)
// =========================================================
public sealed class GetOperationsInspectionUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsInspectionRepository _inspections;

    private readonly IOperationsTrackingRepository _tracking;

    private readonly OperationsParcelReader _reader;

    private readonly OperationsInspectionSelection _selection;

    private readonly IOperationsSettingsRepository _settings;

    public GetOperationsInspectionUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsInspectionRepository inspections,
        IOperationsTrackingRepository tracking,
        OperationsParcelReader reader,
        OperationsInspectionSelection selection,
        IOperationsSettingsRepository settings)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _tracking = tracking ?? throw new ArgumentNullException(nameof(tracking));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
        _selection = selection ?? throw new ArgumentNullException(nameof(selection));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
    }

    public async Task<GetOperationsInspectionResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsInspectionRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return GetOperationsInspectionResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        if (await _inspections.GetInspectionAsync(request.InspectionId, cancellationToken) is not { } inspection
            || await _tracking.GetParcelAsync(inspection.ParcelId, cancellationToken) is not { } parcel)
        {
            return GetOperationsInspectionResponse.Failure(OperationsInspectionErrors.NotFound);
        }

        // The inspector of the warehouse, or whoever manages its holds (a disposal to approve); anyone else: not found.
        OperationsWarehouse? warehouse = await _scope.CoveredAsync(access, OperationsPermissions.InspectionPerform, inspection.WarehouseId, cancellationToken)
            ?? await _scope.CoveredAsync(access, OperationsPermissions.HoldsManage, inspection.WarehouseId, cancellationToken);

        if (warehouse is null)
        {
            return GetOperationsInspectionResponse.Failure(OperationsInspectionErrors.NotFound);
        }

        OperationsInspectionSubject subject = await _selection.SubjectAsync(parcel, cancellationToken);
        IReadOnlyList<OperationsRestrictedItem> items = await _inspections.ListItemsAsync(cancellationToken);
        OperationsParcelSummary summary = (await _reader.SummariesAsync(new[] { parcel }, cancellationToken))[0];
        string currency = (await _settings.FindAsync(cancellationToken))?.CurrencyCode ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.DefaultCurrency;

        return GetOperationsInspectionResponse.Success(inspection, summary, await _reader.ShipmentAsync(parcel.ShipmentId, 0, cancellationToken),
            OperationsInspectionSelector.Applicable(subject, items), OperationsInspectionSelector.Named(subject, items).Select(item => item.Id).ToList(),
            access.CoversWarehouse(OperationsPermissions.InspectionPerform, warehouse.Id, warehouse.BranchId), access.IsOwner || access.IsAdministrator, currency);
    }
}
