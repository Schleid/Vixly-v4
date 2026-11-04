using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Dispatch;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.AddOperationsDispatchLeg;

// =========================================================
// FILE: AddOperationsDispatchLegUseCase.cs
//
// PURPOSE:
// Add a leg to a dispatch before it leaves (Slice 6B, D-4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// =========================================================
public sealed class AddOperationsDispatchLegUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public AddOperationsDispatchLegUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        IOperationsSettingsRepository settings,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<AddOperationsDispatchLegResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        AddOperationsDispatchLegRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return AddOperationsDispatchLegResponse.Failure(refused);
        }

        if (await _dispatches.GetDispatchAsync(request.DispatchId, cancellationToken) is not { } dispatch || warehouses.All(item => item.Id != dispatch.WarehouseId))
        {
            return AddOperationsDispatchLegResponse.Failure(OperationsDispatchErrors.NotFound);
        }

        OperationsCarrierKind kind = request.Kind;

        if (request.CarrierId is { } carrierId)
        {
            if (await _dispatches.GetCarrierAsync(carrierId, cancellationToken) is not { } carrier)
            {
                return AddOperationsDispatchLegResponse.Failure(OperationsDispatchErrors.CarrierInvalid);
            }

            if (!carrier.IsActive)
            {
                return AddOperationsDispatchLegResponse.Failure(OperationsDispatchErrors.CarrierInactive);
            }

            kind = carrier.Kind;
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        OperationsDispatchLeg leg;

        try
        {
            leg = dispatch.AddLeg(new OperationsDispatchLegDraft(kind, request.CarrierId, request.From, request.To,
                request.Departure is { } departs ? OperationsTimeZones.At(departs, settings.TimeZone) : null, request.Arrival is { } arrives ? OperationsTimeZones.At(arrives, settings.TimeZone) : null,
                request.Reference, request.Driver, request.Vehicle) { RequestKey = request.RequestKey }, _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return AddOperationsDispatchLegResponse.Failure(exception.Error);
        }

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return AddOperationsDispatchLegResponse.Success(leg.Id);
        }

        _unitOfWork.DiscardChanges();
        return AddOperationsDispatchLegResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
