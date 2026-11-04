using Vixly.Operations.Application.Bundles;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Application.Settings;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Bundles;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Bundles.CreateOperationsBundle;

// =========================================================
// FILE: CreateOperationsBundleUseCase.cs
//
// PURPOSE:
// Open a new bundle for a destination and a service (Slice 6A, D-2).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - ops.bundles.manage, at the origin warehouse
// - A volumetric target needs the service's divisor
// =========================================================
public sealed class CreateOperationsBundleUseCase
{
    private readonly OperationsBundleDesk _desk;

    private readonly IOperationsBundleRepository _bundles;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsShippingServiceRepository _services;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsNumbering _numbering;

    private readonly OperationsBundling _bundling;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public CreateOperationsBundleUseCase(
        OperationsBundleDesk desk,
        IOperationsBundleRepository bundles,
        IOperationsLocationRepository locations,
        IOperationsShippingServiceRepository services,
        IOperationsSettingsRepository settings,
        OperationsNumbering numbering,
        OperationsBundling bundling,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _desk = desk ?? throw new ArgumentNullException(nameof(desk));
        _bundles = bundles ?? throw new ArgumentNullException(nameof(bundles));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _numbering = numbering ?? throw new ArgumentNullException(nameof(numbering));
        _bundling = bundling ?? throw new ArgumentNullException(nameof(bundling));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<CreateOperationsBundleResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        CreateOperationsBundleRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);
        (OperationsAccessView? access, IReadOnlyList<OperationsWarehouse> warehouses, Error? refused) = await _desk.OpenAsync(context, new[] { OperationsPermissions.BundlesManage }, cancellationToken);

        if (refused is not null)
        {
            return CreateOperationsBundleResponse.Failure(refused);
        }

        if (warehouses.FirstOrDefault(item => item.Id == request.WarehouseId) is not { } warehouse)
        {
            return CreateOperationsBundleResponse.Failure(OperationsAccessErrors.PermissionRequired);
        }

        // A double click: the bundle the first click made (ADR-0019).
        if (request.RequestKey is { } key && await _bundles.FindBundleByKeyAsync(key, cancellationToken) is { } made)
        {
            return CreateOperationsBundleResponse.Success(made.Id);
        }

        Vixly.Operations.Domain.Settings.OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken)
            ?? Vixly.Operations.Domain.Settings.OperationsTenantSettings.CreateDefault(context.RequiredTenantId, _timeProvider.GetUtcNow());
        IReadOnlyList<string> types = settings.BundleTypeList();
        string? type = types.FirstOrDefault(item => string.Equals(item, request.TypeName?.Trim(), StringComparison.OrdinalIgnoreCase));

        // One of the company's types when it has some (owner, A-5); none otherwise.
        if (types.Count > 0 ? type is null : !string.IsNullOrWhiteSpace(request.TypeName))
        {
            return CreateOperationsBundleResponse.Failure(OperationsBundleErrors.TypeInvalid);
        }

        if ((await _services.ListAsync(cancellationToken)).FirstOrDefault(item => item.Offered && item.ServiceCode == request.ServiceCode?.Trim()) is not { } service)
        {
            return CreateOperationsBundleResponse.Failure(OperationsBundleErrors.ServiceRequired);
        }

        string? country = request.CountryCode;

        if (request.DestinationWarehouseId is { } destinationId)
        {
            if (await _locations.GetWarehouseAsync(destinationId, cancellationToken) is not { IsDestination: true } destination)
            {
                return CreateOperationsBundleResponse.Failure(OperationsBundleErrors.DestinationRequired);
            }

            country = destination.Address.CountryCode;
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsBundle bundle;

        try
        {
            OperationsBundleDraft draft = new(warehouse.Id, request.DestinationWarehouseId, country, service.ServiceCode, type, request.Basis,
                request.Target is { } target ? OperationsUnits.ToKilograms(target, settings.WeightUnit) : null, service.VolumetricDivisor);
            bundle = OperationsBundle.Create(context.RequiredTenantId, draft, await _numbering.NextAsync(OperationsNumberKind.Bundle, cancellationToken), access!.User.Id, request.RequestKey, utcNow);
        }
        catch (DomainException exception)
        {
            return CreateOperationsBundleResponse.Failure(exception.Error);
        }

        _bundles.Add(bundle);
        await _bundling.BundleEventAsync(bundle, OperationsTrackingCodes.BundleCreated, new Dictionary<string, string>(StringComparer.Ordinal), access.User.Id, null, cancellationToken);

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return CreateOperationsBundleResponse.Success(bundle.Id);
        }

        _unitOfWork.DiscardChanges();
        return request.RequestKey is { } again && await _bundles.FindBundleByKeyAsync(again, cancellationToken) is { } first
            ? CreateOperationsBundleResponse.Success(first.Id)
            : CreateOperationsBundleResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
