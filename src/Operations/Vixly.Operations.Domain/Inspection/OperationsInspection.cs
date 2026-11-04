using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Inspection;

// =========================================================
// FILE: OperationsInspection.cs
//
// PURPOSE:
// One compliance inspection of a parcel, from the queue to its outcome (Slice 3C design section 5).
//
// RESPONSIBILITIES:
// - Record why the parcel was chosen, who inspected it and when
// - Record the outcome: listed items found, notes, what the customer must do, the fee
// - Keep a disposal waiting until the Owner or an administrator, not the inspector, approves it (Q-C3)
//
// IMPORTANT NOTES:
// - The tracking events (ADR-0021) tell the parcel's story; this record keeps the details they do not carry
// - At most one open inspection per parcel (filtered unique index)
// - A fee is typed by the inspector until Slice 4 and paid with the shipment's invoice (Q-C4)
// =========================================================
public sealed class OperationsInspection : OperationsEntity
{
    // A held parcel waits for one of these from the customer (design section 5).
    public static IReadOnlyList<OperationsHoldReasons> CustomerHolds { get; } = new[]
    {
        OperationsHoldReasons.InspectionDocuments, OperationsHoldReasons.InspectionPermit, OperationsHoldReasons.InspectionValue, OperationsHoldReasons.InspectionFee
    };

    private OperationsInspection()
    {
        Why = string.Empty;
    }

    private OperationsInspection(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
        Why = string.Empty;
    }

    public Guid ParcelId { get; private set; }

    public Guid WarehouseId { get; private set; }

    // rule, random or flagged (OperationsTrackingCodes.InspectionReasons).
    public string Why { get; private set; }

    // The rule or listed item that matched, or the clerk's reason for a flag.
    public string? Reason { get; private set; }

    public Guid? SelectedByUserId { get; private set; }

    public OperationsInspectionStatus Status { get; private set; }

    public bool IsOpen => Status != OperationsInspectionStatus.Done;

    public Guid? StartedByUserId { get; private set; }

    public DateTimeOffset? StartedUtc { get; private set; }

    public Guid? FinishedByUserId { get; private set; }

    public DateTimeOffset? FinishedUtc { get; private set; }

    public string? Result { get; private set; }

    // The names of the listed items found inside, as they read at the time.
    public string? ItemsFound { get; private set; }

    public string? Notes { get; private set; }

    public string? CustomerAction { get; private set; }

    public OperationsHoldReasons HoldReason { get; private set; }

    public decimal? Fee { get; private set; }

    public string? FeeCurrency { get; private set; }

    public DateTimeOffset? CalledCustomerUtc { get; private set; }

    public string? CallNotes { get; private set; }

    public Guid? ApprovedByUserId { get; private set; }

    public DateTimeOffset? ApprovedUtc { get; private set; }

    public static OperationsInspection Select(Guid tenantId, OperationsParcel parcel, string why, string? reason, Guid? selectedBy, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(parcel);

        if (parcel.WarehouseId is not { } warehouse || parcel.TenantId != tenantId)
        {
            throw new DomainException(OperationsInspectionErrors.ParcelNotHere);
        }

        if (!OperationsTrackingCodes.InspectionReasons.Contains(why, StringComparer.Ordinal))
        {
            throw new DomainException(OperationsInspectionErrors.ResultInvalid);
        }

        return new OperationsInspection(tenantId, utcNow)
        {
            ParcelId = parcel.Id,
            WarehouseId = warehouse,
            Why = why,
            Reason = why == "flagged"
                ? OperationsDeclarationText.Required(reason, OperationsInspectionLimits.ReasonMaxLength, OperationsInspectionErrors.FlagReasonRequired)
                : Shorten(reason),
            SelectedByUserId = selectedBy,
            Status = OperationsInspectionStatus.Waiting
        };
    }

    // Opening the box. The same inspector pressing twice is one start.
    public void Start(Guid userId, DateTimeOffset utcNow)
    {
        if (Status == OperationsInspectionStatus.InProgress && StartedByUserId == userId)
        {
            return;
        }

        if (Status != OperationsInspectionStatus.Waiting)
        {
            throw new DomainException(OperationsInspectionErrors.NotWaiting);
        }

        (Status, StartedByUserId, StartedUtc) = (OperationsInspectionStatus.InProgress, userId, utcNow);
        MarkAsModified(utcNow);
    }

    public void Finish(Guid userId, OperationsInspectionOutcome outcome, string currencyCode, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(outcome);

        if (Status != OperationsInspectionStatus.InProgress)
        {
            throw new DomainException(OperationsInspectionErrors.NotInProgress);
        }

        if (!OperationsTrackingCodes.InspectionResults.Contains(outcome.Result, StringComparer.Ordinal))
        {
            throw new DomainException(OperationsInspectionErrors.ResultInvalid);
        }

        bool held = outcome.Result == "held";
        string? notes = OperationsDeclarationText.Optional(outcome.Notes, OperationsInspectionLimits.NotesMaxLength);
        string? action = OperationsDeclarationText.Optional(outcome.CustomerAction, OperationsInspectionLimits.CustomerActionMaxLength);
        string? items = outcome.ItemsFound.Count == 0 ? null : string.Join(", ", outcome.ItemsFound.Select(item => item.Trim()).Where(item => item.Length > 0).Distinct(StringComparer.Ordinal));

        if (items is { Length: > OperationsInspectionLimits.ItemsFoundMaxLength })
        {
            throw new DomainException(OperationsInspectionErrors.TextInvalid);
        }

        if (outcome.Result == "passed_with_notes" && notes is null)
        {
            throw new DomainException(OperationsInspectionErrors.NotesRequired);
        }

        if (held && !CustomerHolds.Contains(outcome.HoldReason))
        {
            throw new DomainException(OperationsInspectionErrors.HoldReasonInvalid);
        }

        if (held && action is null)
        {
            throw new DomainException(OperationsInspectionErrors.CustomerActionRequired);
        }

        decimal? fee = held && outcome.HoldReason == OperationsHoldReasons.InspectionFee ? outcome.Fee : null;

        if (held && outcome.HoldReason == OperationsHoldReasons.InspectionFee && fee is not (> 0 and <= OperationsInspectionLimits.MaxFee))
        {
            throw new DomainException(OperationsInspectionErrors.FeeInvalid);
        }

        // Disposal: the customer was called first, then the company's own policy (Q-C3).
        bool disposal = outcome.Result == "refused_disposed";
        string? call = disposal ? OperationsDeclarationText.Optional(outcome.CallNotes, OperationsInspectionLimits.CallNotesMaxLength) : null;

        if (disposal && (outcome.CalledCustomerUtc is not { } called || called > utcNow.AddDays(1) || call is null))
        {
            throw new DomainException(OperationsInspectionErrors.CallRequired);
        }

        (Result, ItemsFound, Notes, CustomerAction) = (outcome.Result, items, notes, held || outcome.Result.StartsWith("refused", StringComparison.Ordinal) ? action : null);
        (HoldReason, Fee, FeeCurrency) = (held ? outcome.HoldReason : OperationsHoldReasons.None, fee is null ? null : decimal.Round(fee.Value, 2, MidpointRounding.AwayFromZero), fee is null ? null : currencyCode);
        (CalledCustomerUtc, CallNotes) = (disposal ? outcome.CalledCustomerUtc : null, call);
        (FinishedByUserId, FinishedUtc) = (userId, utcNow);
        Status = disposal ? OperationsInspectionStatus.AwaitingApproval : OperationsInspectionStatus.Done;
        MarkAsModified(utcNow);
    }

    // The Owner or an administrator, never an inspector of this parcel (who opened the box or asked for the disposal).
    public void ApproveDisposal(Guid approverId, bool ownerOrAdministrator, DateTimeOffset utcNow)
    {
        CheckApprover(approverId, ownerOrAdministrator);
        (ApprovedByUserId, ApprovedUtc, Status) = (approverId, utcNow, OperationsInspectionStatus.Done);
        MarkAsModified(utcNow);
    }

    // Not disposed of: back to the inspector for another outcome.
    public void DeclineDisposal(Guid approverId, bool ownerOrAdministrator, DateTimeOffset utcNow)
    {
        CheckApprover(approverId, ownerOrAdministrator);
        (Status, Result, FinishedByUserId, FinishedUtc, CalledCustomerUtc, CallNotes) = (OperationsInspectionStatus.InProgress, null, null, null, null, null);
        MarkAsModified(utcNow);
    }

    private void CheckApprover(Guid approverId, bool ownerOrAdministrator)
    {
        if (Status != OperationsInspectionStatus.AwaitingApproval)
        {
            throw new DomainException(OperationsInspectionErrors.NotAwaitingApproval);
        }

        if (!ownerOrAdministrator)
        {
            throw new DomainException(OperationsInspectionErrors.ApproverNotAllowed);
        }

        if (approverId == FinishedByUserId || approverId == StartedByUserId)
        {
            throw new DomainException(OperationsInspectionErrors.SameApprover);
        }
    }

    private static string? Shorten(string? reason)
    {
        string? text = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();
        return text is { Length: > OperationsInspectionLimits.ReasonMaxLength } ? text[..OperationsInspectionLimits.ReasonMaxLength] : text;
    }
}
