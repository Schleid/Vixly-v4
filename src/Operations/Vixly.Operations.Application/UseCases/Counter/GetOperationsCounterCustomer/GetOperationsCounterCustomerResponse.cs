using Vixly.Operations.Application.Counter;
using Vixly.Operations.Application.Shipments;
using Vixly.Operations.Domain.Locations;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterCustomer;

// =========================================================
// FILE: GetOperationsCounterCustomerResponse.cs
//
// PURPOSE:
// Shows a customer at the counter: who they are, all their shipments, and what a new shipment can be (Slice 5C, C-5 and C-6).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCounterCustomerResponse
{
    private GetOperationsCounterCustomerResponse()
    {
    }

    public bool Succeeded { get; private init; }

    // Slice 5E: the customer's latest orders of supplies on their own.
    public IReadOnlyList<Vixly.Operations.Domain.Supplies.OperationsSupplySale> Orders { get; private init; } = Array.Empty<Vixly.Operations.Domain.Supplies.OperationsSupplySale>();

    public Error? Error { get; private init; }

    public OperationsCounterCustomer? Customer { get; private init; }

    public IReadOnlyList<OperationsCounterShipment> Shipments { get; private init; } = Array.Empty<OperationsCounterShipment>();

    public IReadOnlyList<OperationsChoice> Origins { get; private init; } = Array.Empty<OperationsChoice>();

    public IReadOnlyList<OperationsWarehouse> Destinations { get; private init; } = Array.Empty<OperationsWarehouse>();

    public IReadOnlyList<string> Services { get; private init; } = Array.Empty<string>();

    // Slice 5D: the insurance products the company sells, in its order, with the currency they are priced in.
    public IReadOnlyList<Vixly.Operations.Domain.Charges.OperationsInsurancePlan> Insurance { get; private init; } = Array.Empty<Vixly.Operations.Domain.Charges.OperationsInsurancePlan>();

    public bool InsuranceOffered => Insurance.Count > 0;

    public string CurrencyCode { get; private init; } = string.Empty;

    public DateTimeOffset UtcNow { get; private init; }

    public Vixly.Operations.Domain.Settings.OperationsDeliveryModes DeliveryModes { get; private init; }

    // Where the customer stands (owner, 2026-10-20).
    public OperationsCounterStanding? Standing { get; private init; }

    public static GetOperationsCounterCustomerResponse Success(OperationsCounterCustomer? customer, IReadOnlyList<OperationsCounterShipment> shipments, IReadOnlyList<OperationsChoice> origins, IReadOnlyList<OperationsWarehouse> destinations, IReadOnlyList<string> services, IReadOnlyList<Vixly.Operations.Domain.Charges.OperationsInsurancePlan> insurance, Vixly.Operations.Domain.Settings.OperationsDeliveryModes deliveryModes, OperationsCounterStanding? standing = null, string currencyCode = "", DateTimeOffset utcNow = default, IReadOnlyList<Vixly.Operations.Domain.Supplies.OperationsSupplySale>? orders = null)
    {
        return new GetOperationsCounterCustomerResponse { Orders = orders ?? Array.Empty<Vixly.Operations.Domain.Supplies.OperationsSupplySale>(), Succeeded = true, Standing = standing, Customer = customer, Shipments = shipments, Origins = origins, Destinations = destinations, Services = services, Insurance = insurance, DeliveryModes = deliveryModes, CurrencyCode = currencyCode, UtcNow = utcNow };
    }

    public static GetOperationsCounterCustomerResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCounterCustomerResponse { Error = error };
    }
}
