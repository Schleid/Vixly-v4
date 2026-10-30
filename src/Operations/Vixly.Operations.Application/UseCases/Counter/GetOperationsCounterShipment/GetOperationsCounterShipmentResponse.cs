using Vixly.Operations.Application.Counter;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Counter.GetOperationsCounterShipment;

// =========================================================
// FILE: GetOperationsCounterShipmentResponse.cs
//
// PURPOSE:
// Shows a shipment at the counter: its parcels against the count, and the amount so far or, once final, its amount (Slice 5C, C-7 and C-8).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsCounterShipmentResponse
{
    private GetOperationsCounterShipmentResponse()
    {
    }

    public bool Succeeded { get; private init; }

    // Slice 5F: its invoice once issued.
    public Vixly.Operations.Domain.Billing.OperationsInvoice? Invoice { get; private set; }

    internal GetOperationsCounterShipmentResponse Invoiced(Vixly.Operations.Domain.Billing.OperationsInvoice? invoice)
    {
        Invoice = invoice;
        return this;
    }

    // Slice 5E: the supplies added to the shipment at the counter.
    public Vixly.Operations.Domain.Supplies.OperationsSupplySale? Supplies { get; private init; }

    public Error? Error { get; private init; }

    public OperationsCounterCustomer? Customer { get; private init; }

    public OperationsCounterShipment? Shipment { get; private init; }

    public IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsParcel> Parcels { get; private init; } = Array.Empty<Vixly.Operations.Domain.Tracking.OperationsParcel>();

    public Vixly.Operations.Application.Charges.OperationsRating? Estimate { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public Vixly.Operations.Domain.Settings.OperationsWeightUnit WeightUnit { get; private init; }

    public Vixly.Operations.Domain.Settings.OperationsDimensionUnit DimensionUnit { get; private init; }

    // Slice 5D: the insurance products the shipment may have, what each would cost for its parcels, the total without insurance, and whether staff may change it.
    public IReadOnlyList<Vixly.Operations.Domain.Charges.OperationsInsurancePlan> Insurance { get; private init; } = Array.Empty<Vixly.Operations.Domain.Charges.OperationsInsurancePlan>();

    public IReadOnlyDictionary<Guid, decimal?>? Premiums { get; private init; }

    public decimal? BaseTotal { get; private init; }

    public bool StaffMayChangeInsurance { get; private init; }

    // Slice 5D: the shipment estimate beside the page.
    public Vixly.Operations.Application.Charges.OperationsEstimate? Panel { get; private init; }

    public DateTimeOffset UtcNow { get; private init; }

    // Where the customer stands (owner, 2026-10-20).
    public OperationsCounterStanding? Standing { get; private init; }

    public static GetOperationsCounterShipmentResponse Success(Vixly.Operations.Domain.Supplies.OperationsSupplySale? supplies, OperationsCounterCustomer? customer, OperationsCounterShipment? shipment, IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsParcel> parcels, Vixly.Operations.Application.Charges.OperationsRating? estimate, string currencyCode, Vixly.Operations.Domain.Settings.OperationsWeightUnit weightUnit, Vixly.Operations.Domain.Settings.OperationsDimensionUnit dimensionUnit, OperationsCounterStanding? standing = null,
        IReadOnlyList<Vixly.Operations.Domain.Charges.OperationsInsurancePlan>? insurance = null, IReadOnlyDictionary<Guid, decimal?>? premiums = null, decimal? baseTotal = null,
        bool staffMayChangeInsurance = false, DateTimeOffset utcNow = default, Vixly.Operations.Application.Charges.OperationsEstimate? panel = null)
    {
        return new GetOperationsCounterShipmentResponse { Supplies = supplies, Succeeded = true, Standing = standing, Insurance = insurance ?? Array.Empty<Vixly.Operations.Domain.Charges.OperationsInsurancePlan>(), Premiums = premiums, BaseTotal = baseTotal,
            StaffMayChangeInsurance = staffMayChangeInsurance, UtcNow = utcNow, Panel = panel, Customer = customer, Shipment = shipment, Parcels = parcels, Estimate = estimate, CurrencyCode = currencyCode, WeightUnit = weightUnit, DimensionUnit = dimensionUnit };
    }

    public static GetOperationsCounterShipmentResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsCounterShipmentResponse { Error = error };
    }
}
