using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.OpenOperationsRepackBox;

// =========================================================
// FILE: OpenOperationsRepackBoxUseCase.cs
//
// PURPOSE:
// Start repacking a consolidated shipment into one box (Slice 6A, D-4).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.fill, at the shipment's origin warehouse
// - One box per shipment at a time (a unique index on the live box)
// =========================================================
public sealed class OpenOperationsRepackBoxUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsReceivingRepository _receiving;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsNumbering _numbering;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public OpenOperationsRepackBoxUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsReceivingRepository receiving,
        IOperationsSettingsRepository settings,
        OperationsNumbering numbering,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _receiving = receiving ?? throw new ArgumentNullException(nameof(receiving));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<OpenOperationsRepackBoxResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        OpenOperationsRepackBoxRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesFill }, cancellationToken);

        if (refused is not null)
        {
            return OpenOperationsRepackBoxResponse.Failure(refused);
        }

        if (await _receiving.GetShipmentAsync(request.ShipmentId, cancellationToken) is not { } shipment || warehouses.All(item => item.Id != shipment.OriginWarehouseId))
        {
            return OpenOperationsRepackBoxResponse.Failure(OperationsBundleErrors.NotFound);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());

        if (!shipment.Options.HasFlag(OperationsShipmentOptions.Consolidate) || shipment.WayOr(settings.ConsolidationWay) != OperationsConsolidationWay.Repack)
        {
            return OpenOperationsRepackBoxResponse.Failure(OperationsBundleErrors.NotRepacked);
        }

        // Its box already (a double click, or another worker): the same box.
        if (await _bundles.FindLiveBoxAsync(shipment.Id, cancellationToken) is { } live)
        {
            return OpenOperationsRepackBoxResponse.Success(live.Id);
        }

        OperationsRepackBox box;

        try
        {
            box = OperationsRepackBox.Open(context.RequiredTenantId, await _numbering.NextAsync(OperationsNumberKind.Box, cancellationToken), shipment, access!.User.Id, request.RequestKey,
                _timeProvider.GetUtcNow());
        }
        catch (DomainException exception)
        {
            return OpenOperationsRepackBoxResponse.Failure(exception.Error);
        }

        _bundles.Add(box);
        shipment.StartConsolidating(settings.ConsolidationWay, _timeProvider.GetUtcNow());

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return OpenOperationsRepackBoxResponse.Success(box.Id);
        }

        _unitOfWork.DiscardChanges();
        return await _bundles.FindLiveBoxAsync(shipment.Id, cancellationToken) is { } made
            ? OpenOperationsRepackBoxResponse.Success(made.Id)
            : OpenOperationsRepackBoxResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
