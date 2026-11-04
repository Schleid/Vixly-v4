using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Dispatch;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Dispatch;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Dispatch.CreateOperationsDispatch;

// =========================================================
// FILE: CreateOperationsDispatchUseCase.cs
//
// PURPOSE:
// Plan a dispatch from a warehouse to a destination by a service (Slice 6B, D-2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.dispatches.manage at the dispatch's origin warehouse (D-8)
// =========================================================
public sealed class CreateOperationsDispatchUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsDispatchRepository _dispatches;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsShippingServiceRepository _services;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsNumbering _numbering;

    private readonly OperationsDispatching _dispatching;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CreateOperationsDispatchUseCase(
        OperationsBundleDesk desk,
        IOperationsDispatchRepository dispatches,
        IOperationsLocationRepository locations,
        IOperationsShippingServiceRepository services,
        IOperationsSettingsRepository settings,
        OperationsNumbering numbering,
        OperationsDispatching dispatching,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _dispatches = dispatches ?? throw new ArgumentNullException(nameof(dispatches));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _dispatching = dispatching ?? throw new ArgumentNullException(nameof(dispatching));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CreateOperationsDispatchResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CreateOperationsDispatchRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.DispatchesManage }, cancellationToken);

        if (refused is not null)
        {
            return CreateOperationsDispatchResponse.Failure(refused);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } warehouse)
        {
            return CreateOperationsDispatchResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // A double click: the dispatch the first click made (ADR-0019).
        if (request.RequestKey is { } key && await _dispatches.FindDispatchByKeyAsync(key, cancellationToken) is { } made)
        {
            return CreateOperationsDispatchResponse.Success(made.Id);
        }

        if ((await _services.ListAsync(cancellationToken)).FirstOrDefault(item => item.Offered && item.ServiceCode == request.ServiceCode?.Trim()) is not { } service)
        {
            return CreateOperationsDispatchResponse.Failure(OperationsDispatchErrors.ServiceRequired);
        }

        string? country = request.CountryCode;
        Guid? receiving = null;
        bool onward = false;

        if (request.DestinationWarehouseId is { } destinationId)
        {
            if (await _locations.GetWarehouseAsync(destinationId, cancellationToken) is not { IsDestination: true } destination)
            {
                return CreateOperationsDispatchResponse.Failure(OperationsDispatchErrors.DestinationRequired);
            }

            country = destination.Address.CountryCode;

            // Slice 7 (R-11, review): from the hub that serves it, or from a warehouse in its country, it goes straight there (onward);
            // otherwise to the hub that serves it when it has one. Fixed now: a hub changed later does not move dispatches already planned.
            onward = warehouse.Id == destination.HubWarehouseId || string.Equals(warehouse.Address.CountryCode, country, StringComparison.OrdinalIgnoreCase);
            receiving = !onward && destination.HubWarehouseId is { } hub && hub != destination.Id ? hub : destination.Id;
        }
        else
        {
            onward = string.Equals(warehouse.Address.CountryCode, country?.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsDispatch dispatch;

        try
        {
            // Slice 7 (R-11): from a warehouse in the destination country (a hub), it is an onward dispatch: it goes to its destination, not to a hub.
            OperationsDispatchDraft draft = new(warehouse.Id, request.DestinationWarehouseId, country, service.ServiceCode,
                request.Cutoff is { } cutoff ? OperationsTimeZones.At(cutoff, settings.TimeZone) : null, request.Notes)
                { Onward = onward, ReceivingWarehouseId = receiving };
            dispatch = OperationsDispatch.Create(context.RequiredTenantId, draft, await _numbering.NextAsync(OperationsNumberKind.Dispatch, cancellationToken), access!.User.Id, request.RequestKey, utcNow);
        }
        catch (DomainException exception)
        {
            return CreateOperationsDispatchResponse.Failure(exception.Error);
        }

        _dispatches.Add(dispatch);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CreateOperationsDispatchResponse.Success(dispatch.Id);
        }

        _unitOfWork.DiscardChanges();
        return request.RequestKey is { } again && await _dispatches.FindDispatchByKeyAsync(again, cancellationToken) is { } first
            ? CreateOperationsDispatchResponse.Success(first.Id)
            : CreateOperationsDispatchResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
