using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Dispatch;

// =========================================================
// FILE: OperationsDispatchLeg.cs
//
// PURPOSE:
// One leg of a dispatch: a flight, a sailing or a truck (Slice 6B, D-4).
//
// RESPONSIBILITIES:
// - Hold its kind, carrier, places, scheduled and actual times, and its reference (MAWB, B/L, truck or tracking number)
//
// IMPORTANT NOTES:
// - Kept with its dispatch (JSON); changed only through the dispatch
// =========================================================
public sealed class OperationsDispatchLeg
{
    public const int PlaceMaxLength = 80;

    public const int ReferenceMaxLength = 40;

    private OperationsDispatchLeg()
    {
        From = string.Empty;
        To = string.Empty;
    }

    internal OperationsDispatchLeg(int sequence, OperationsDispatchLegDraft draft)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (!Enum.IsDefined(draft.Kind))
        {
            throw new DomainException(OperationsDispatchErrors.CarrierInvalid);
        }

        if (draft.DepartureUtc is { } departs && draft.ArrivalUtc is { } arrives && arrives < departs)
        {
            throw new DomainException(OperationsDispatchErrors.LegTimeInvalid);
        }

        Id = Guid.NewGuid();
        Sequence = sequence;
        Kind = draft.Kind;
        CarrierId = draft.CarrierId;
        From = OperationsDispatchText.Required(draft.From, PlaceMaxLength, OperationsDispatchErrors.TextInvalid);
        To = OperationsDispatchText.Required(draft.To, PlaceMaxLength, OperationsDispatchErrors.TextInvalid);
        ScheduledDepartureUtc = draft.DepartureUtc;
        ScheduledArrivalUtc = draft.ArrivalUtc;
        Reference = OperationsDispatchText.Optional(draft.Reference, ReferenceMaxLength);
        Driver = OperationsDispatchText.Optional(draft.Driver, PlaceMaxLength);
        Vehicle = OperationsDispatchText.Optional(draft.Vehicle, ReferenceMaxLength);
        Status = OperationsDispatchLegStatus.Planned;
        RequestKey = draft.RequestKey;
    }

    public Guid Id { get; private set; }

    public int Sequence { get; private set; }

    public OperationsCarrierKind Kind { get; private set; }

    public Guid? CarrierId { get; private set; }

    public string From { get; private set; }

    public string To { get; private set; }

    public DateTimeOffset? ScheduledDepartureUtc { get; private set; }

    public DateTimeOffset? ScheduledArrivalUtc { get; private set; }

    public string? Reference { get; private set; }

    public string? Driver { get; private set; }

    public string? Vehicle { get; private set; }

    public OperationsDispatchLegStatus Status { get; private set; }

    public Guid? RequestKey { get; private set; }

    public DateTimeOffset? DepartedUtc { get; private set; }

    public DateTimeOffset? ArrivedUtc { get; private set; }

    internal void Renumber(int sequence) => Sequence = sequence;

    internal void Depart(DateTimeOffset at) => (Status, DepartedUtc) = (OperationsDispatchLegStatus.Departed, at);

    internal void Arrive(DateTimeOffset at) => (Status, ArrivedUtc) = (OperationsDispatchLegStatus.Arrived, at);
}
