using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Charges;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Shipments.GetMyOperationsShipmentChoices;

// =========================================================
// FILE: GetMyOperationsShipmentChoicesResponse.cs
//
// PURPOSE:
// What a customer may choose when creating a shipment (owner, 2026-10-18).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsShipmentChoicesResponse
{
    private GetMyOperationsShipmentChoicesResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public IReadOnlyList<OperationsChoice> Origins { get; private init; } = Array.Empty<OperationsChoice>();

    public IReadOnlyList<OperationsChoice> Branches { get; private init; } = Array.Empty<OperationsChoice>();

    public bool Pickup { get; private init; }

    public bool HomeDelivery { get; private init; }

    public string RecipientName { get; private init; } = string.Empty;

    public string RecipientPhone { get; private init; } = string.Empty;

    public IReadOnlyList<string> Countries { get; private init; } = Array.Empty<string>();

    // Slice 4C: the destination warehouses the customer chooses from ("Deliver to").
    public IReadOnlyList<OperationsWarehouse> Destinations { get; private init; } = Array.Empty<OperationsWarehouse>();

    // Slice 4C: the insurance plans in use, whose terms the customer accepts when insuring.
    public IReadOnlyList<OperationsInsurancePlan> Insurance { get; private init; } = Array.Empty<OperationsInsurancePlan>();

    public DateTimeOffset UtcNow { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    // Slice 4D: the services priced for the customer, to choose from (none: the company chooses).
    public IReadOnlyList<string> Services { get; private init; } = Array.Empty<string>();

    // Slice 5B: what the company owes, per mode, when the shipment is not insured.
    public IReadOnlyList<Vixly.Operations.Application.Insurance.OperationsLiabilityView> Liability { get; private set; } = Array.Empty<Vixly.Operations.Application.Insurance.OperationsLiabilityView>();

    public GetMyOperationsShipmentChoicesResponse WithLiability(IReadOnlyList<Vixly.Operations.Application.Insurance.OperationsLiabilityView> liability)
    {
        Liability = liability ?? throw new ArgumentNullException(nameof(liability));
        return this;
    }

    public static GetMyOperationsShipmentChoicesResponse Success(IReadOnlyList<OperationsChoice> origins, IReadOnlyList<OperationsChoice> branches, bool pickup, bool homeDelivery, string recipientName, string recipientPhone, IReadOnlyList<string> countries,
        IReadOnlyList<OperationsWarehouse>? destinations = null, IReadOnlyList<OperationsInsurancePlan>? insurance = null, DateTimeOffset utcNow = default, string currencyCode = "", IReadOnlyList<string>? services = null)
    {
        return new GetMyOperationsShipmentChoicesResponse
        {
            Succeeded = true, Origins = origins, Branches = branches, Pickup = pickup, HomeDelivery = homeDelivery, RecipientName = recipientName, RecipientPhone = recipientPhone, Countries = countries,
            Destinations = destinations ?? Array.Empty<OperationsWarehouse>(), Insurance = insurance ?? Array.Empty<OperationsInsurancePlan>(), UtcNow = utcNow, CurrencyCode = currencyCode, Services = services ?? Array.Empty<string>()
        };
    }

    public static GetMyOperationsShipmentChoicesResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsShipmentChoicesResponse { Error = error };
    }
}
