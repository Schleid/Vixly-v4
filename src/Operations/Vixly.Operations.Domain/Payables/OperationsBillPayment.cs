namespace Vixly.Operations.Domain.Payables;

// =========================================================
// FILE: OperationsBillPayment.cs
//
// PURPOSE:
// A payment the company made on a bill (Slice 8).
//
// RESPONSIBILITIES:
// - Keep the day, amount, method, reference and who recorded it
//
// IMPORTANT NOTES:
// - Owned by its bill; recorded, never sent by Vixly
// =========================================================
public sealed class OperationsBillPayment
{
    public const int ReferenceMaxLength = 60;

    private OperationsBillPayment()
    {
    }

    public OperationsBillPayment(DateOnly paidOn, decimal amount, OperationsBillPaymentMethod method, string? reference, Guid recordedBy, DateTimeOffset utcNow)
    {
        Id = Guid.NewGuid();
        PaidOn = paidOn;
        Amount = OperationsPayableText.Money(amount, OperationsPayableErrors.AmountInvalid);
        Method = Enum.IsDefined(method) ? method : throw new Vixly.Shared.Kernel.Domain.DomainException(OperationsPayableErrors.AmountInvalid);
        Reference = OperationsPayableText.Optional(reference, ReferenceMaxLength);
        (RecordedByUserId, RecordedUtc) = (recordedBy, utcNow);
    }

    public Guid Id { get; private set; }

    public DateOnly PaidOn { get; private set; }

    public decimal Amount { get; private set; }

    public OperationsBillPaymentMethod Method { get; private set; }

    public string? Reference { get; private set; }

    public Guid RecordedByUserId { get; private set; }

    public DateTimeOffset RecordedUtc { get; private set; }
}
