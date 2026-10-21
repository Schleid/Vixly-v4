using Vixly.Operations.Application.Customers;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Customers;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Operations.Domain.Shipments;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.Shipments;

// =========================================================
// FILE: OperationsShipmentIntake.cs
//
// PURPOSE:
// Turns what a customer typed into shipment instructions, and lists what they may choose (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Offer the customer's mailbox warehouses as origins and the active destination branches as pickup points
// - Check the delivery mode against the company's settings, the branch, the address and the recipient
//
// IMPORTANT NOTES:
// - Pickup or home delivery only when the company offers it (tenant settings, Slice 1)
// - Never trusts an id from the form: every branch and warehouse is looked up again
// =========================================================
public sealed class OperationsShipmentIntake
{
    private readonly IOperationsLocationRepository _locations;

    private readonly IOperationsSettingsRepository _settings;

    private readonly IOperationsChargeRepository _charges;

    private readonly TimeProvider _timeProvider;

    public OperationsShipmentIntake(IOperationsLocationRepository locations, IOperationsSettingsRepository settings, IOperationsChargeRepository charges, TimeProvider timeProvider)
    {
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _locations = locations ?? throw new ArgumentNullException(nameof(locations));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _charges = charges ?? throw new ArgumentNullException(nameof(charges));
    }

    // Slice 4C: the destination warehouses the customer can choose: in use, with pickup there or home delivery from there.
    public async Task<IReadOnlyList<OperationsWarehouse>> DestinationsAsync(CancellationToken cancellationToken) =>
        (await _locations.ListWarehousesAsync(cancellationToken)).Where(warehouse => warehouse.IsDestination)
            .OrderBy(warehouse => warehouse.Address.CountryCode, StringComparer.Ordinal).ThenBy(warehouse => warehouse.Name, StringComparer.CurrentCulture).ToList();

    // Slice 4C: the insurance products in use with their terms in force, which the customer accepts when insuring; Slice 5D: in the company's order.
    public async Task<IReadOnlyList<OperationsInsurancePlan>> InsuranceAsync(CancellationToken cancellationToken) =>
        (await _charges.ListInsurancePlansAsync(cancellationToken)).Where(plan => plan.IsActive && plan.VersionAt(_timeProvider.GetUtcNow()) is not null)
            .OrderBy(plan => plan.Mode).ThenBy(plan => plan.Position).ThenBy(plan => plan.Name, StringComparer.CurrentCulture).ToList();

    // Slice 5D: the products a shipment of this service may have; while the service is not chosen, those of the mode it is priced in (air; review of 5D).
    public static IReadOnlyList<OperationsInsurancePlan> ProductsFor(IReadOnlyList<OperationsInsurancePlan> products, string? serviceCode) =>
        products.Where(plan => plan.Mode == OperationsTransportModes.Of(string.IsNullOrWhiteSpace(serviceCode) ? null : serviceCode.Trim())).ToList();

    public DateTimeOffset UtcNow => _timeProvider.GetUtcNow();

    public async Task<string> CurrencyAsync(CancellationToken cancellationToken) =>
        (await _settings.FindAsync(cancellationToken))?.CurrencyCode ?? OperationsTenantSettings.DefaultCurrency;

    public async Task<OperationsDeliveryModes> DeliveryModesAsync(CancellationToken cancellationToken) =>
        (await _settings.FindAsync(cancellationToken))?.DeliveryModes ?? OperationsDeliveryModes.Pickup;

    // The warehouses where the customer has a mailbox address.
    public async Task<IReadOnlyList<OperationsChoice>> OriginsAsync(OperationsCustomer customer, CancellationToken cancellationToken) =>
        OperationsMailboxAddresses.For(customer, await _locations.ListWarehousesAsync(cancellationToken))
            .Select(mailbox => new OperationsChoice(mailbox.WarehouseId, mailbox.WarehouseName, mailbox.City + ", " + mailbox.CountryCode))
            .ToList();

    public async Task<IReadOnlyList<OperationsChoice>> PickupBranchesAsync(CancellationToken cancellationToken) =>
        (await _locations.ListBranchesAsync(cancellationToken))
            .Where(branch => branch.Status == OperationsLocationStatus.Active && branch.Side != OperationsBranchSide.Origin)
            .OrderBy(branch => branch.Address.CountryCode, StringComparer.Ordinal).ThenBy(branch => branch.Name, StringComparer.CurrentCulture)
            .Select(branch => new OperationsChoice(branch.Id, branch.Name, branch.Address.City + ", " + branch.Address.CountryCode))
            .ToList();

    public async Task<IReadOnlyList<string>> DestinationCountriesAsync(CancellationToken cancellationToken) =>
        (await _locations.ListBranchesAsync(cancellationToken))
            .Where(branch => branch.Status == OperationsLocationStatus.Active && branch.Side != OperationsBranchSide.Origin)
            .Select(branch => branch.Address.CountryCode).Distinct(StringComparer.Ordinal).Order(StringComparer.Ordinal).ToList();

    // Slice 4D: the services priced for the customer (its own lists and the standard ones), in the platform's order.
    public async Task<IReadOnlyList<string>> ServicesAsync(Guid customerId, CancellationToken cancellationToken)
    {
        HashSet<string> priced = (await _charges.ListCardsForAsync(customerId, cancellationToken)).Where(card => card.IsActive).Select(card => card.ServiceCode).ToHashSet(StringComparer.Ordinal);
        return Vixly.Contracts.Platform.Catalog.PlatformShippingServices.All.Select(service => service.Code).Where(priced.Contains).ToList();
    }

    // recordedBy: at the counter, the clerk who records the customer's acceptance of the insurance terms (Slice 5D).
    public async Task<(OperationsShipmentInstructions? Instructions, Error? Error)> InstructionsAsync(OperationsShipmentDraft draft, CancellationToken cancellationToken,
        Guid? customerId = null, Guid? recordedBy = null)
    {
        ArgumentNullException.ThrowIfNull(draft);
        (OperationsShipmentInstructions? instructions, Error? error) = await BuildAsync(draft, cancellationToken);

        // Slice 4D: a service the company prices for the customer; none lets the company choose (Q-D1).
        if (instructions is not null && !string.IsNullOrWhiteSpace(draft.Service))
        {
            if (customerId is not { } customer || !(await ServicesAsync(customer, cancellationToken)).Contains(draft.Service.Trim(), StringComparer.Ordinal))
            {
                return (null, OperationsShipmentErrors.ServiceInvalid);
            }

            instructions.ForService(draft.Service);
        }

        // Slice 5D: insured with one of the products offered for its service; with a single product, that one.
        if (instructions is not null && draft.Insured)
        {
            IReadOnlyList<OperationsInsurancePlan> offered = await InsuranceAsync(cancellationToken);
            IReadOnlyList<OperationsInsurancePlan> eligible = ProductsFor(offered, instructions.ServiceCode);
            OperationsInsurancePlan? chosen = draft.InsurancePlanId is { } planId ? eligible.FirstOrDefault(plan => plan.Id == planId) : eligible.Count == 1 ? eligible[0] : null;

            if (chosen is null && offered.Count > 0)
            {
                return (null, OperationsShipmentErrors.InsuranceChoiceRequired);
            }

            instructions.ForInsurance(chosen?.Id);
        }

        // The acceptance given with this draft is recorded on the shipment with its time, and who recorded it at the counter.
        return (draft.InsuranceAccepted ? instructions?.AcceptTerms(recordedBy) : instructions, error);
    }

    private async Task<(OperationsShipmentInstructions? Instructions, Error? Error)> BuildAsync(OperationsShipmentDraft draft, CancellationToken cancellationToken)
    {
        OperationsDeliveryModes modes = await DeliveryModesAsync(cancellationToken);
        OperationsShipmentOptions options = (draft.Consolidate ? OperationsShipmentOptions.Consolidate : OperationsShipmentOptions.None)
            | (draft.Refrigerated ? OperationsShipmentOptions.Refrigerated : OperationsShipmentOptions.None)
            | (draft.Insured ? OperationsShipmentOptions.Insured : OperationsShipmentOptions.None);

        try
        {
            // Slice 4C: insured only with the insurance terms accepted.
            if (draft.Insured && !draft.InsuranceAccepted && (await InsuranceAsync(cancellationToken)).Count > 0)
            {
                return (null, OperationsShipmentErrors.InsuranceTermsRequired);
            }

            // Slice 4C: the customer chooses the destination warehouse when the company has destinations; else the destination branches of Slice 3C.
            IReadOnlyList<OperationsWarehouse> destinations = await DestinationsAsync(cancellationToken);

            if (destinations.Count > 0)
            {
                return ToDestination(draft, destinations, modes, options);
            }

            switch (draft.Delivery)
            {
                case "pickup" when modes.HasFlag(OperationsDeliveryModes.Pickup):
                    return draft.BranchId is { } branchId && await _locations.GetBranchAsync(branchId, cancellationToken) is { } branch
                        ? (OperationsShipmentInstructions.Pickup(branch, draft.RecipientName, draft.RecipientPhone, options), null)
                        : (null, OperationsShipmentErrors.DestinationInvalid);
                case "home" when modes.HasFlag(OperationsDeliveryModes.HomeDelivery):
                    OperationsPostalAddress address = OperationsPostalAddress.Create(draft.Line1, draft.Line2, draft.City, draft.Region, draft.PostalCode, draft.CountryCode);

                    // Only to a country the company serves.
                    if (!(await DestinationCountriesAsync(cancellationToken)).Contains(address.CountryCode, StringComparer.Ordinal))
                    {
                        return (null, OperationsShipmentErrors.DestinationInvalid);
                    }

                    return (OperationsShipmentInstructions.HomeDelivery(address, draft.RecipientName, draft.RecipientPhone, options), null);
                default:
                    return (null, OperationsShipmentErrors.DeliveryModeUnavailable);
            }
        }
        catch (DomainException exception)
        {
            return (null, exception.Error);
        }
    }

    private static (OperationsShipmentInstructions? Instructions, Error? Error) ToDestination(OperationsShipmentDraft draft, IReadOnlyList<OperationsWarehouse> destinations,
        OperationsDeliveryModes modes, OperationsShipmentOptions options)
    {
        if (destinations.FirstOrDefault(warehouse => warehouse.Id == draft.DestinationId) is not { } destination)
        {
            return (null, OperationsShipmentErrors.DestinationInvalid);
        }

        switch (draft.Delivery)
        {
            case "pickup" when modes.HasFlag(OperationsDeliveryModes.Pickup) && destination.Offers(OperationsDeliveryModes.Pickup):
                return (OperationsShipmentInstructions.Pickup(destination, draft.RecipientName, draft.RecipientPhone, options), null);
            case "home" when modes.HasFlag(OperationsDeliveryModes.HomeDelivery) && destination.Offers(OperationsDeliveryModes.HomeDelivery):
                OperationsPostalAddress address = OperationsPostalAddress.Create(draft.Line1, draft.Line2, draft.City, draft.Region, draft.PostalCode, draft.CountryCode);
                return (OperationsShipmentInstructions.HomeDelivery(destination, address, draft.RecipientName, draft.RecipientPhone, options), null);
            default:
                return (null, OperationsShipmentErrors.DeliveryModeUnavailable);
        }
    }
}
