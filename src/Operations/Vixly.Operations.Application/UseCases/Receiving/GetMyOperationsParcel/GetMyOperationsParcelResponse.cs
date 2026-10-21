using Vixly.Operations.Application.Receiving;
using Vixly.Operations.Application.Tracking;
using Vixly.Operations.Domain.Settings;
using Vixly.Shared.Kernel.Errors;

namespace Vixly.Operations.Application.UseCases.Receiving.GetMyOperationsParcel;

// =========================================================
// FILE: GetMyOperationsParcelResponse.cs
//
// PURPOSE:
// One of a customer's own packages: history and photos (Slice 3B).
//
// RESPONSIBILITIES:
// - Report success with its values, or the error code
//
// IMPORTANT NOTES:
// - Failure carries a code, never text (L-1)
// =========================================================
public sealed class GetMyOperationsParcelResponse
{
    private GetMyOperationsParcelResponse()
    {
    }

    public bool Succeeded { get; private init; }

    public Error? Error { get; private init; }

    public OperationsCustomerParcel? Parcel { get; private init; }

    public IReadOnlyList<OperationsTrackingLine> History { get; private init; } = Array.Empty<OperationsTrackingLine>();

    public IReadOnlyList<OperationsParcelPhotoInfo> Photos { get; private init; } = Array.Empty<OperationsParcelPhotoInfo>();

    public string? ReceiptNumber { get; private init; }

    public OperationsWeightUnit WeightUnit { get; private init; }

    public OperationsDimensionUnit DimensionUnit { get; private init; }

    public static GetMyOperationsParcelResponse Success(OperationsCustomerParcel? parcel, IReadOnlyList<OperationsTrackingLine> history, IReadOnlyList<OperationsParcelPhotoInfo> photos, string? receiptNumber, OperationsWeightUnit weightUnit, OperationsDimensionUnit dimensionUnit,
        string? actionNeeded = null, decimal? fee = null, string? feeCurrency = null,
        IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsHoldReasons>? answerable = null, IReadOnlyList<Vixly.Operations.Application.Holds.OperationsHoldAnswerView>? answers = null)
    {
        return new GetMyOperationsParcelResponse
        {
            Succeeded = true, Parcel = parcel, History = history, Photos = photos, ReceiptNumber = receiptNumber, WeightUnit = weightUnit, DimensionUnit = dimensionUnit,
            ActionNeeded = actionNeeded, Fee = fee, FeeCurrency = feeCurrency,
            Answerable = answerable ?? Array.Empty<Vixly.Operations.Domain.Tracking.OperationsHoldReasons>(), Answers = answers ?? Array.Empty<Vixly.Operations.Application.Holds.OperationsHoldAnswerView>()
        };
    }

    // What the inspector asked of the customer while the parcel is held or refused (Slice 3C); answering on the portal comes with 3E.
    public string? ActionNeeded { get; private init; }

    public decimal? Fee { get; private init; }

    public string? FeeCurrency { get; private init; }

    // Slice 4D step 3: the storage fee owed so far (0 when none), charged with the shipment.
    public decimal StorageFee { get; private init; }

    public string StorageCurrency { get; private init; } = string.Empty;

    public GetMyOperationsParcelResponse WithStorageFee(decimal fee, string currency) =>
        new()
        {
            Succeeded = Succeeded, Error = Error, Parcel = Parcel, History = History, Photos = Photos, ReceiptNumber = ReceiptNumber, WeightUnit = WeightUnit, DimensionUnit = DimensionUnit,
            ActionNeeded = ActionNeeded, Fee = Fee, FeeCurrency = FeeCurrency, Answerable = Answerable, Answers = Answers, StorageFee = fee, StorageCurrency = currency
        };

    // The holds the customer can answer online now, and their latest answers (Slice 3E).
    public IReadOnlyList<Vixly.Operations.Domain.Tracking.OperationsHoldReasons> Answerable { get; private init; } = Array.Empty<Vixly.Operations.Domain.Tracking.OperationsHoldReasons>();

    public IReadOnlyList<Vixly.Operations.Application.Holds.OperationsHoldAnswerView> Answers { get; private init; } = Array.Empty<Vixly.Operations.Application.Holds.OperationsHoldAnswerView>();

    public static GetMyOperationsParcelResponse Failure(Error error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new GetMyOperationsParcelResponse { Error = error };
    }
}
