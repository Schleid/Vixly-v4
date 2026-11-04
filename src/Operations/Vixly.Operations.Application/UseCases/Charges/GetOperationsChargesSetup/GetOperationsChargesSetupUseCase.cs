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

namespace Vixly.Operations.Application.UseCases.Charges.GetOperationsChargesSetup;

// =========================================================
// FILE: GetOperationsChargesSetupUseCase.cs
//
// PURPOSE:
// The company's price lists, extras, insurance plan and item types (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Run the rule and return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetOperationsChargesSetupUseCase
{
    private readonly OperationsAccess _access;

    private readonly IOperationsChargeRepository _charges;

    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsCustomerRepository _customers;

    private readonly IOperationsSettingsRepository _settings;

    private readonly TimeProvider _timeProvider;

    public GetOperationsChargesSetupUseCase(
        OperationsAccess access,
        IOperationsChargeRepository charges,
        IOperationsLocationRepository locations,
        IOperationsCustomerRepository customers,
        IOperationsSettingsRepository settings,
        TimeProvider timeProvider)
    {
        _access = access ?? throw new ArgumentNullException(nameof(access));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _customers = customers ?? throw new ArgumentNullException(nameof(customers));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<GetOperationsChargesSetupResponse> ExecuteAsync(
        OperationsAuthorizationContext context,
        GetOperationsChargesSetupRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        (OperationsAccessView? _, Error? refused) = await _access.RequireAsync(context, OperationsPermissions.ChargesConfigure, false, cancellationToken);

        if (refused is not null)
        {
            return GetOperationsChargesSetupResponse.Failure(refused);
        }

        IReadOnlyList<OperationsRateCard> cards = await _charges.ListCardsAsync(cancellationToken);
        IReadOnlyList<OperationsWarehouse> warehouses = await _locations.ListWarehousesAsync(cancellationToken);
        Dictionary<Guid, OperationsCustomer> customers = (await _customers.ListByIdsAsync(cards.Where(card => card.CustomerId is not null).Select(card => card.CustomerId!.Value).Distinct().ToList(),
            cancellationToken)).ToDictionary(customer => customer.Id);
        DateTimeOffset utcNow = _timeProvider.GetUtcNow();
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(context.RequiredTenantId, utcNow);

        return GetOperationsChargesSetupResponse.Success(
            cards.Select(card => new OperationsRateCardRow(card, warehouses.FirstOrDefault(warehouse => warehouse.Id == card.OriginWarehouseId)?.Name,
                warehouses.FirstOrDefault(warehouse => warehouse.Id == card.DestinationWarehouseId)?.Name,
                card.CustomerId is { } id && customers.TryGetValue(id, out OperationsCustomer? customer) ? customer.Code : null,
                card.CustomerId is { } owner && customers.TryGetValue(owner, out OperationsCustomer? named) ? named.FullName : null)).ToList(),
            await _charges.ListExtrasAsync(cancellationToken),
            await _charges.ListInsurancePlansAsync(cancellationToken),
            PlatformShippingServices.All.Select(service => service.Code).ToList(),
            await _charges.ListItemTypesAsync(cancellationToken),
            warehouses.Where(warehouse => warehouse.IsOrigin).ToList(),
            warehouses.Where(warehouse => warehouse.IsDestination).ToList(),
            settings.WeightUnit,
            settings.DimensionUnit,
            settings.CurrencyCode,
            settings.TimeZone,
            utcNow);
    }
}
