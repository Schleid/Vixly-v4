using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Charges;

// =========================================================
// FILE: OperationsRater.cs
//
// PURPOSE:
// Loads what a rating needs in one go (Slice 4 and 4B).
//
// RESPONSIBILITIES:
// - Read the settings, services, price lists (standard and the customer's), extras, insurance plan, zones and item types
//
// IMPORTANT NOTES:
// - Read only; never another customer's price list
// =========================================================
public sealed class OperationsRater
{
    private readonly IOperationsChargeRepository _charges;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsShippingServiceRepository _services;

    private readonly IOperationsLocationRepository _locations;

    private readonly TimeProvider _timeProvider;

    public OperationsRater(IOperationsChargeRepository charges, IOperationsSettingsRepository settings, IOperationsShippingServiceRepository services, IOperationsLocationRepository locations,
        TimeProvider timeProvider)
    {
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public async Task<OperationsRatingSetup> LoadAsync(Guid tenantId, Guid? customerId, CancellationToken cancellationToken)
    {
        OperationsTenantSettings settings = await _settings.FindAsync(cancellationToken) ?? OperationsTenantSettings.CreateDefault(tenantId, _timeProvider.GetUtcNow());

        return new OperationsRatingSetup(
            settings,
            await _services.ListAsync(cancellationToken),
            await _charges.ListCardsForAsync(customerId, cancellationToken),
            (await _charges.ListExtrasAsync(cancellationToken)).Where(extra => extra.IsActive).ToList(),
            await _charges.ListInsurancePlansAsync(cancellationToken),
            await _locations.ListWarehousesAsync(cancellationToken),
            await _charges.ListItemTypesAsync(cancellationToken));
    }
}
