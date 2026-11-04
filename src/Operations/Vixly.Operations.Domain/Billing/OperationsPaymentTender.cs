namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsPaymentTender.cs
//
// PURPOSE:
// One part of a payment: a method and its amount (Slice 5F, C-3, C-5 and C-6).
//
// RESPONSIBILITIES:
// - Keep the method, the amount, the cash given and the change, the reference
// - Keep whether the money is in, and who checked it
//
// IMPORTANT NOTES:
// - Zelle and bank transfers wait for finance; every other method is settled at once
// =========================================================
public sealed class OperationsPaymentTender
{
    private OperationsPaymentTender()
    {
    }

    internal OperationsPaymentTender(OperationsPaymentMethod method, decimal amount, decimal? cashGiven, string? reference)
    {
        (Id, Method, Amount, Reference) = (Guid.NewGuid(), method, amount, reference);

        if (method == OperationsPaymentMethod.Cash)
        {
            CashGiven = cashGiven ?? amount;
            Change = CashGiven - amount;
        }

        Status = NeedsConfirmation(method) ? OperationsTenderStatus.AwaitingConfirmation : OperationsTenderStatus.Settled;
    }

    public Guid Id { get; private set; }

    public OperationsPaymentMethod Method { get; private set; }

    public decimal Amount { get; private set; }

    public decimal? CashGiven { get; private set; }

    public decimal? Change { get; private set; }

    // A cheque number, a transfer reference, the Zelle confirmation number when the customer shows it.
    public string? Reference { get; private set; }

    public OperationsTenderStatus Status { get; private set; }

    public Guid? CheckedByUserId { get; private set; }

    public DateTimeOffset? CheckedUtc { get; private set; }

    // The bank's reference when received; why when not received.
    public string? CheckNote { get; private set; }

    public static bool NeedsConfirmation(OperationsPaymentMethod method) => method is OperationsPaymentMethod.Zelle or OperationsPaymentMethod.BankTransfer;

    internal void Checked(bool received, string? note, Guid byUserId, DateTimeOffset utcNow) =>
        (Status, CheckNote, CheckedByUserId, CheckedUtc) = (received ? OperationsTenderStatus.Settled : OperationsTenderStatus.NotReceived, note, byUserId, utcNow);
}
