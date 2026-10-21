using Vixly.Contracts.Platform.Catalog;
using Vixly.Operations.Application.Audit;
using Vixly.Operations.Application.Charges;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Application.Security;
using Vixly.Operations.Domain.Access;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Charges.SaveOperationsPriceList;

// =========================================================
// FILE: SaveOperationsPriceListUseCase.cs
//
// PURPOSE:
// Adds a price list, or schedules a new version of one, and takes it in or out of use (Slice 4B design section 5).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class SaveOperationsPriceListUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsChargeRepository _charges;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsAuditTrail _audit;

    private readonly IOperationsUnitOfWork _unitOfWork;

    private readonly TimeProvider _timeProvider;

    public SaveOperationsPriceListUseCase(
        OperationsAccess access,
        IOperationsChargeRepository charges,
        IOperationsLocationRepository locations,
        IOperationsCustomerRepository customers,
        IOperationsSettingsRepository settings,
        OperationsAuditTrail audit,
        IOperationsUnitOfWork unitOfWork,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _audit = audit ?? throw new ArgumentNullException(nameof(audit));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<SaveOperationsPriceListResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        SaveOperationsPriceListRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? access, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ChargesConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return SaveOperationsPriceListResponse.Failure(refused);
        }

        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);
        IReadOnlyList<OperationsItemType> types = await _charges.ListItemTypesAsync(cancellationToken);
        OperationsRatePrices prices = new(request.Bands ?? Array.Empty<OperationsRateBand>(), request.Items ?? Array.Empty<OperationsRateItem>(), request.MinimumCharge, request.ServiceFee,
            request.ConsolidationFee, request.PerPackage, request.PerShipment);

        // Item prices name the company's own item types only.
        if (prices.Items.Any(item => types.All(type => type.Id != item.ItemTypeId)))
        {
            return SaveOperationsPriceListResponse.Failure(OperationsChargeErrors.ItemTypeNotFound);
        }

        OperationsRateCard card;
        string action;

        try
        {
            DateTimeOffset? start = OperationsCharging.Start(request.EffectiveFrom, settings, utcNow);

            if (request.CardId is { } cardId)
            {
                card = await _charges.GetCardAsync(cardId, cancellationToken) ?? throw new DomainException(OperationsChargeErrors.CardNotFound);

                // The same prices as the form showed (the current version, else the first scheduled one), from now: only the status changes
                // (a second click, or a list taken out of use); a scheduled version never starts early this way.
                bool same = start is null && (card.VersionAt(utcNow) ?? card.Versions.FirstOrDefault()) is { } shown && OperationsCharging.Same(shown, prices);
                action = same ? "ops.charges.price_list_status" : "ops.charges.price_version_scheduled";

                if (!same)
                {
                    card.Schedule(prices, start, utcNow);
                }

                card.ChangeCustoms(request.Customs, utcNow);
                card.ChangeStatus(request.Active, utcNow);
            }
            else
            {
                if (PlatformShippingServices.Find(request.ServiceCode?.Trim()) is not { } service)
                {
                    return SaveOperationsPriceListResponse.Failure(OperationsChargeErrors.ServiceUnknown);
                }

                // Slice 4C: the route runs from one of the company's origins (drop-off) to one of its destinations, or any.
                IReadOnlyList<OperationsWarehouse> warehouses = await _locations.ListWarehousesAsync(cancellationToken);

                if ((request.OriginWarehouseId is { } originId && !warehouses.Any(warehouse => warehouse.Id == originId && warehouse.IsOrigin))
                    || (request.DestinationWarehouseId is { } destinationId && !warehouses.Any(warehouse => warehouse.Id == destinationId && warehouse.IsDestination)))
                {
                    return SaveOperationsPriceListResponse.Failure(OperationsChargeErrors.RouteInvalid);
                }

                Guid? customerId = null;

                if (!string.IsNullOrWhiteSpace(request.CustomerCode))
                {
                    customerId = (await _customers.FindByCodeAsync(request.CustomerCode.Trim().ToUpperInvariant(), cancellationToken))?.Id
                        ?? throw new DomainException(OperationsChargeErrors.CustomerNotFound);
                }

                if ((await _charges.ListCardsAsync(cancellationToken)).Count >= OperationsChargeLimits.MaxCards)
                {
                    return SaveOperationsPriceListResponse.Failure(OperationsChargeErrors.TooMany);
                }

                card = OperationsRateCard.Create(context.RequiredTenantId, service.Code, request.OriginWarehouseId, request.DestinationWarehouseId, customerId, request.Customs, prices,
                    start, utcNow);

                if (await _charges.FindCardAsync(card.ServiceCode, card.OriginWarehouseId, card.DestinationWarehouseId, card.CustomerId, cancellationToken) is not null)
                {
                    return SaveOperationsPriceListResponse.Failure(OperationsChargeErrors.CardDuplicate);
                }

                _charges.Add(card);
                action = "ops.charges.price_list_added";
            }
        }
        catch (DomainException exception)
        {
            return SaveOperationsPriceListResponse.Failure(exception.Error);
        }

        OperationsPriceVersion? latest = card.Versions.LastOrDefault();
        _audit.Record(access!.User.Id, action, nameof(OperationsRateCard), card.Id, after: new
        {
            service = card.ServiceCode, origin = card.OriginWarehouseId, destination = card.DestinationWarehouseId, customer = card.CustomerId, basis = card.Basis.ToString(),
            customs = card.Customs.ToString(), active = card.IsActive, perPackage = latest?.PerPackage, perShipment = latest?.PerShipment,
            effectiveFrom = latest?.EffectiveFromUtc, bands = latest?.Bands.Select(band => new { band.From, kind = band.Kind.ToString(), band.Amount, band.Base }),
            items = latest?.Items.Count, minimum = latest?.MinimumCharge, serviceFee = latest?.ServiceFee, consolidation = latest?.ConsolidationFee
        });

        if (await _unitOfWork.SaveChangesAsync(cancellationToken))
        {
            return SaveOperationsPriceListResponse.Success(card.Id);
        }

        // The same new list sent twice at once: the unique index keeps one (ADR-0019).
        _unitOfWork.DiscardChanges();
        return request.CardId is null && await _charges.FindCardAsync(card.ServiceCode, card.OriginWarehouseId, card.DestinationWarehouseId, card.CustomerId, cancellationToken) is { } raced
            ? SaveOperationsPriceListResponse.Success(raced.Id)
            : SaveOperationsPriceListResponse.Failure(OperationsCommonErrors.ConcurrencyConflict);
    }
}
