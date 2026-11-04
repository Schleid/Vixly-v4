using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Inspection;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.ListOperationsHolds;

// =========================================================
// FILE: ListOperationsHoldsUseCase.cs
//
// PURPOSE:
// Open holds at the warehouses a person covers, oldest first, and the disposals awaiting approval (Slice 3C design section 6).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - Scoped by place: a warehouse grant shows that warehouse only
// - Everyone with the page sees the disposals waiting; only the Owner or an administrator decides
// =========================================================
public sealed class ListOperationsHoldsUseCase
{
    private readonly OperationsOwnerCheck _owner;

    private readonly OperationsReceivingScope _scope;

    private readonly IOperationsInspectionRepository _inspections;

    private readonly OperationsInspectionReader _reader;

    private readonly IOperationsHoldRepository _holds;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public ListOperationsHoldsUseCase(
        OperationsOwnerCheck owner,
        OperationsReceivingScope scope,
        IOperationsInspectionRepository inspections,
        OperationsInspectionReader reader,
        IOperationsHoldRepository holds,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _holds = holds ?? throw new ArgumentNullException(nameof(holds));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _scope = scope ?? throw new ArgumentNullException(nameof(scope));
        _inspections = inspections ?? throw new ArgumentNullException(nameof(inspections));
        _reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public async Task<ListOperationsHoldsResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        ListOperationsHoldsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _owner.LoadAsync(context, cancellationToken) is not { } access)
        {
            return ListOperationsHoldsResponse.Failure(OperationsAccessErrors.SignInRequired);
        }

        IReadOnlyList<OperationsWarehouse> warehouses = await _scope.WarehousesAsync(access, OperationsPermissions.HoldsManage, false, cancellationToken);

        if (warehouses.Count == 0)
        {
            return ListOperationsHoldsResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        List<Guid> shown = warehouses.Where(warehouse => request.WarehouseId is null || warehouse.Id == request.WarehouseId).Select(warehouse => warehouse.Id).ToList();
        OperationsHoldReasons reason = OperationsTrackingCodes.HoldReasonNames.Contains(request.Reason.ToString(), StringComparer.Ordinal) ? request.Reason : OperationsHoldReasons.None;
        (IReadOnlyList<OperationsParcel> parcels, int total) = await _inspections.ListHeldAsync(shown, reason, Math.Max(request.Page, 1), PageSize, cancellationToken);
        bool approver = access.IsOwner || access.IsAdministrator;
        IReadOnlyList<OperationsInspectionQueueItem> approvals = await _reader.WithParcelsAsync(await _inspections.ListAwaitingApprovalAsync(shown, cancellationToken), cancellationToken);

        // Packages waiting for their customer past the company's days (Slice 3E, Q-R6).
        Vixly.Operations.Domain.Settings.OperationsTenantSettings? settings = await _settings.FindAsync(cancellationToken);
        int days = settings?.UnclaimedDays ?? Vixly.Operations.Domain.Holds.OperationsHoldLimits.DefaultUnclaimedDays;
        (IReadOnlyList<OperationsParcel> unclaimed, int unclaimedCount) = await _holds.ListUnclaimedAsync(shown, _timeProvider.GetUtcNow().AddDays(-days), 1, PageSize, cancellationToken);

        return ListOperationsHoldsResponse.Success(await _reader.HeldAsync(parcels, cancellationToken), total, warehouses, approvals, approver,
            await _reader.HeldAsync(unclaimed, cancellationToken), unclaimedCount, days)
            .WithStorage(settings?.StorageFreeDays ?? Vixly.Operations.Domain.Holds.OperationsHoldLimits.DefaultUnclaimedDays, settings?.StorageFeePerDay,
                settings?.CurrencyCode ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.DefaultCurrency);
    }

    public const int PageSize = 25;
    
}
