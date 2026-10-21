using Vixly.Operations.Domain.Common;
using Vixly.Operations.Domain.Inspection;
using Vixly.Operations.Domain.Receiving;
using Vixly.Operations.Domain.Tracking;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Operations.Domain.Holds;

// =========================================================
// FILE: OperationsHoldAnswer.cs
//
// PURPOSE:
// A customer's answer to a hold on their package, waiting for staff (Slice 3E, Q-R7).
//
// RESPONSIBILITIES:
// - Check the answer fits the hold: a document for documents or a permit; contents and value for a value check; a choice for a damaged package
// - Record the staff review: accepted (the hold ends) or declined with a note
//
// IMPORTANT NOTES:
// - A newer answer to the same hold replaces the open one
// - Only the parcel's own customer answers (S-3); the caller checks the session
// - Paying a fee waits for Slice 9
// =========================================================
public sealed class OperationsHoldAnswer : OperationsEntity
{
    public static IReadOnlyList<string> DamageChoices { get; } = new[] { "ship_as_is", "return", "dispose" };

    private OperationsHoldAnswer()
    {
    }

    private OperationsHoldAnswer(Guid tenantId, DateTimeOffset utcNow)
        : base(tenantId, utcNow)
    {
    }

    public Guid ParcelId { get; private set; }

    public Guid CustomerId { get; private set; }

    public OperationsHoldReasons Reason { get; private set; }

    public OperationsHoldAnswerKind Kind { get; private set; }

    public OperationsHoldAnswerStatus Status { get; private set; }

    public string? Contents { get; private set; }

    public decimal? DeclaredValue { get; private set; }

    public string? CurrencyCode { get; private set; }

    public string? Choice { get; private set; }

    public string? Note { get; private set; }

    public string? DocumentKey { get; private set; }

    public string? DocumentType { get; private set; }

    public string? DocumentName { get; private set; }

    public int? DocumentBytes { get; private set; }

    public string? DocumentSha256 { get; private set; }

    public Guid? ReviewedByUserId { get; private set; }

    public DateTimeOffset? ReviewedUtc { get; private set; }

    public string? ReviewNote { get; private set; }

    // The kind of answer a hold takes; null when the customer cannot answer it online.
    public static OperationsHoldAnswerKind? KindFor(OperationsHoldReasons reason) => reason switch
    {
        OperationsHoldReasons.InspectionDocuments or OperationsHoldReasons.InspectionPermit => OperationsHoldAnswerKind.Document,
        OperationsHoldReasons.InspectionValue => OperationsHoldAnswerKind.Value,
        OperationsHoldReasons.Damaged => OperationsHoldAnswerKind.DamageChoice,
        _ => null
    };

    public static OperationsHoldAnswer Create(OperationsParcel parcel, OperationsHoldReasons reason, string? contents, decimal? value, string currencyCode, string? choice, string? note,
        OperationsHoldDocument? document, DateTimeOffset utcNow)
    {
        ArgumentNullException.ThrowIfNull(parcel);

        if (parcel.CustomerId is not { } customer || !parcel.HoldReasons.HasFlag(reason) || KindFor(reason) is not { } kind)
        {
            throw new DomainException(OperationsHoldErrors.NotAnswerable);
        }

        OperationsHoldAnswer answer = new(parcel.TenantId, utcNow)
        {
            ParcelId = parcel.Id,
            CustomerId = customer,
            Reason = reason,
            Kind = kind,
            Status = OperationsHoldAnswerStatus.Open,
            Note = Text(note, OperationsHoldLimits.NoteMaxLength)
        };

        if (kind == OperationsHoldAnswerKind.Document && document is null)
        {
            throw new DomainException(OperationsHoldErrors.DocumentRequired);
        }

        if (kind == OperationsHoldAnswerKind.Value)
        {
            answer.Contents = Text(contents, OperationsReceivingLimits.ContentsMaxLength) ?? throw new DomainException(OperationsHoldErrors.ContentsRequired);
            answer.DeclaredValue = value is >= 0 and <= OperationsReceivingLimits.MaxDeclaredValue
                ? decimal.Round(value.Value, 2, MidpointRounding.AwayFromZero)
                : throw new DomainException(OperationsHoldErrors.ValueInvalid);
            answer.CurrencyCode = currencyCode;
        }

        if (kind == OperationsHoldAnswerKind.DamageChoice)
        {
            answer.Choice = choice is not null && DamageChoices.Contains(choice, StringComparer.Ordinal) ? choice : throw new DomainException(OperationsHoldErrors.ChoiceInvalid);
        }

        if (document is not null)
        {
            (answer.DocumentKey, answer.DocumentType, answer.DocumentName, answer.DocumentBytes, answer.DocumentSha256) =
                (document.StorageKey, document.ContentType, Name(document.FileName), document.Bytes, document.Sha256);
        }

        return answer;
    }

    public void Replace(DateTimeOffset utcNow)
    {
        EnsureOpen();
        Status = OperationsHoldAnswerStatus.Replaced;
        MarkAsModified(utcNow);
    }

    public void Accept(Guid userId, string? note, DateTimeOffset utcNow)
    {
        EnsureOpen();
        (Status, ReviewedByUserId, ReviewedUtc, ReviewNote) = (OperationsHoldAnswerStatus.Accepted, userId, utcNow, Text(note, OperationsHoldLimits.NoteMaxLength));
        MarkAsModified(utcNow);
    }

    // Declined with what is still missing, which the customer reads.
    public void Decline(Guid userId, string? note, DateTimeOffset utcNow)
    {
        EnsureOpen();
        string reason = Text(note, OperationsHoldLimits.NoteMaxLength) ?? throw new DomainException(OperationsHoldErrors.ReviewNoteRequired);
        (Status, ReviewedByUserId, ReviewedUtc, ReviewNote) = (OperationsHoldAnswerStatus.Declined, userId, utcNow, reason);
        MarkAsModified(utcNow);
    }

    private void EnsureOpen()
    {
        if (Status != OperationsHoldAnswerStatus.Open)
        {
            throw new DomainException(OperationsHoldErrors.AnswerNotOpen);
        }
    }

    private static string? Text(string? value, int maxLength)
    {
        try
        {
            return OperationsDeclarationText.Optional(value, maxLength);
        }
        catch (DomainException)
        {
            throw new DomainException(OperationsHoldErrors.TextInvalid);
        }
    }

    // A file name to show staff, without any path.
    private static string Name(string? fileName)
    {
        string name = Path.GetFileName((fileName ?? string.Empty).Replace('\\', '/')).Trim();
        name = new string(name.Where(character => !char.IsControl(character) && character is not ('<' or '>' or '"')).ToArray());
        return name.Length == 0 ? "document" : name.Length > OperationsHoldLimits.FileNameMaxLength ? name[^OperationsHoldLimits.FileNameMaxLength..] : name;
    }
}
