using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Domain.Inspection;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Inspection.GetOperationsInspection;

// =========================================================
// FILE: GetOperationsInspectionResponse.cs
//
// PURPOSE:
// One inspection with its parcel, shipment and the restricted items that apply (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetOperationsInspectionResponse
{
    private GetOperationsInspectionResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsInspection? Inspection { get; private init; }

    public OperationsParcelSummary? Parcel { get; private init; }

    public Vixly.Operations.Application.Shipments.OperationsCustomerShipment? Shipment { get; private init; }

    public IReadOnlyList<OperationsRestrictedItem> Applicable { get; private init; } = Array.Empty<OperationsRestrictedItem>();

    public IReadOnlyList<Guid> Named { get; private init; } = Array.Empty<Guid>();

    public bool CanPerform { get; private init; }

    public bool CanApprove { get; private init; }

    public string CurrencyCode { get; private init; } = string.Empty;

    public static GetOperationsInspectionResponse Success(OperationsInspection inspection, OperationsParcelSummary parcel, Vixly.Operations.Application.Shipments.OperationsCustomerShipment? shipment, IReadOnlyList<OperationsRestrictedItem> applicable, IReadOnlyList<Guid> named, bool canPerform, bool canApprove, string currencyCode)
    {
        return new GetOperationsInspectionResponse { Succeeded = true, Inspection = inspection, Parcel = parcel, Shipment = shipment, Applicable = applicable, Named = named, CanPerform = canPerform, CanApprove = canApprove, CurrencyCode = currencyCode };
    }

    public static GetOperationsInspectionResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetOperationsInspectionResponse { Error = error };
    }
}
