using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;

namespace Vixly.Platform.Application.UseCases.Payments;

// =========================================================
// FILE: PlatformCardRefunds.cs
//
// PURPOSE:
// Works out how much can still be refunded to the card, per credit note (D-P6).
//
// RESPONSIBILITIES:
// - Limit each refund by the credit note, what is owed back, and what was charged by card and not yet refunded
//
// IMPORTANT NOTES:
// - Refunds already sent or waiting count against the limits; failed ones do not
// =========================================================
public static class PlatformCardRefunds
{
    public static IReadOnlyList<PlatformRefundableCreditItem> Refundable(PlatformInvoice invoice, IReadOnlyList<PlatformCardTransaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(invoice);
        ArgumentNullException.ThrowIfNull(transactions);

        decimal onCard = RefundableOnCard(transactions);
        decimal owed = invoice.RefundOwed - Waiting(transactions);
        decimal room = Math.Min(onCard, owed);

        if (room <= 0m)
        {
            return Array.Empty<PlatformRefundableCreditItem>();
        }

        return invoice.Credits
            .Where(credit => credit.Status == PlatformCreditNoteStatus.Issued && credit.Number is not null)
            .Select(credit => new PlatformRefundableCreditItem(credit.Id, credit.Number!, Math.Min(room, credit.Amount - Used(transactions, credit.Id))))
            .Where(item => item.Refundable > 0m)
            .ToArray();
    }

    // The succeeded charge with the most left to refund.
    public static (PlatformCardTransaction? Charge, decimal Left) BestCharge(IReadOnlyList<PlatformCardTransaction> transactions)
    {
        ArgumentNullException.ThrowIfNull(transactions);

        return transactions
            .Where(item => item.Kind == PlatformCardTransactionKind.Charge && item.Status == PlatformCardTransactionStatus.Succeeded && item.Dispute is null)
            .Select(item => ((PlatformCardTransaction?)item, item.Amount - Counting(transactions).Where(refund => refund.ChargeTransactionId == item.Id).Sum(refund => refund.Amount)))
            .OrderByDescending(pair => pair.Item2)
            .FirstOrDefault();
    }

    private static decimal RefundableOnCard(IReadOnlyList<PlatformCardTransaction> transactions)
    {
        return BestCharge(transactions).Left;
    }

    private static IEnumerable<PlatformCardTransaction> Counting(IReadOnlyList<PlatformCardTransaction> transactions)
    {
        return transactions.Where(item => item.Kind == PlatformCardTransactionKind.Refund && item.Status != PlatformCardTransactionStatus.Failed && item.Status != PlatformCardTransactionStatus.Cancelled);
    }

    private static decimal Waiting(IReadOnlyList<PlatformCardTransaction> transactions)
    {
        return Counting(transactions).Where(item => item.Origin == PlatformCardTransactionOrigin.Refund && item.IsOpen).Sum(item => item.Amount);
    }

    private static decimal Used(IReadOnlyList<PlatformCardTransaction> transactions, Guid creditId)
    {
        return Counting(transactions).Where(item => item.CreditNoteId == creditId).Sum(item => item.Amount);
    }
}
