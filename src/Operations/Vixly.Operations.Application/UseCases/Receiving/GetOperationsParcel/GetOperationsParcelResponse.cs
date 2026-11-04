using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Printing;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetOperationsParcel;

// =========================================================
// FILE: GetOperationsParcelResponse.cs
//
// PURPOSE:
// One parcel for staff: details, customer, shipment, history, photos and receipt (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsParcelResponse
{
    private GetOperationsParcelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsParcelSummary? Parcel { get; private init; }

    public IReadOnlyList<OperationsTrackingLine> History { get; private init; } = Array.Empty<OperationsTrackingLine>();

    public IReadOnlyList<OperationsParcelPhotoInfo> Photos { get; private init; } = Array.Empty<OperationsParcelPhotoInfo>();

    public string? ReceiptNumber { get; private init; }

    public string? Condition { get; private init; }

    public IReadOnlyList<OperationsStorageLocation> Locations { get; private init; } = Array.Empty<OperationsStorageLocation>();

    public bool CanAssign { get; private init; }

    public bool CanPutAway { get; private init; }

    public bool CanCheck { get; private init; }

    public string? CustomerPhone { get; private init; }

    public IReadOnlyList<OperationsParcelSummary> ShipmentParcels { get; private init; } = Array.Empty<OperationsParcelSummary>();

    public IReadOnlyList<OperationsLabelPrinter> Printers { get; private init; } = Array.Empty<OperationsLabelPrinter>();

    public OperationsWeightUnit WeightUnit { get; private init; }

    public OperationsDimensionUnit DimensionUnit { get; private init; }

    // Where the parcel's shipment goes and who receives it, as the customer wrote it; null when it has no shipment.
    public Vixly.Operations.Application.Shipments.OperationsCustomerShipment? Shipment { get; private init; }

    // The parcel's latest compliance inspection, if it was chosen for one (Slice 3C).
    public Vixly.Operations.Domain.Inspection.OperationsInspection? Inspection { get; private init; }

    // Slice 4D step 2: cleared, so its shipping label can be printed.
    public bool ShippingCleared { get; private init; }

    // Slice 4D step 3: the customer whose mailbox code is written on an undeclared package's label (staff check before assigning).
    public Vixly.Operations.Domain.Customers.OperationsCustomer? LabelCustomer { get; private init; }

    // The name is shown only to staff who may see customers (review of 4D step 3).
    public bool LabelCustomerNamed { get; private init; }

    // Slice 4D step 3: the storage fee owed so far, in the company's currency (0 when none).
    public decimal StorageFee { get; private init; }

    public int StorageDays { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public GetOperationsParcelResponse WithStorage(Vixly.Operations.Domain.Customers.OperationsCustomer? labelCustomer, decimal fee, int days, string currencyCode, bool named = false) =>
        new()
        {
            Succeeded = Succeeded, Error = Error, Parcel = Parcel, History = History, Photos = Photos, ReceiptNumber = ReceiptNumber, Condition = Condition, Locations = Locations, CanAssign = CanAssign,
            CanPutAway = CanPutAway, CanCheck = CanCheck, CustomerPhone = CustomerPhone, ShipmentParcels = ShipmentParcels, Printers = Printers, WeightUnit = WeightUnit, DimensionUnit = DimensionUnit,
            Shipment = Shipment, Inspection = Inspection, ShippingCleared = ShippingCleared, LabelCustomer = labelCustomer, LabelCustomerNamed = named, StorageFee = fee, StorageDays = days, CurrencyCode = currencyCode
        };

    public static GetOperationsParcelResponse Success(OperationsParcelSummary? parcel, IReadOnlyList<OperationsTrackingLine> history, IReadOnlyList<OperationsParcelPhotoInfo> photos, string? receiptNumber, string? condition, IReadOnlyList<OperationsStorageLocation> locations, bool canAssign, bool canPutAway, bool canCheck, string? customerPhone, IReadOnlyList<OperationsParcelSummary> shipmentParcels, IReadOnlyList<OperationsLabelPrinter> printers, OperationsWeightUnit weightUnit, OperationsDimensionUnit dimensionUnit, Vixly.Operations.Application.Shipments.OperationsCustomerShipment? shipment, Vixly.Operations.Domain.Inspection.OperationsInspection? inspection = null, bool shippingCleared = false)
    {
        return new GetOperationsParcelResponse { Succeeded = true, Parcel = parcel, History = history, Photos = photos, ReceiptNumber = receiptNumber, Condition = condition, Locations = locations, CanAssign = canAssign, CanPutAway = canPutAway, CanCheck = canCheck, CustomerPhone = customerPhone, ShipmentParcels = shipmentParcels, Printers = printers, WeightUnit = weightUnit, DimensionUnit = dimensionUnit, Shipment = shipment, Inspection = inspection, ShippingCleared = shippingCleared };
    }

    public static GetOperationsParcelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsParcelResponse { Error = error };
    }
}
