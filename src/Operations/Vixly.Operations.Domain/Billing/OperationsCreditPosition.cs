namespace Vixly.Operations.Domain.Billing;

// =========================================================
// FILE: OperationsCreditPosition.cs
//
// PURPOSE:
// Where a credit account stands today (Slice 5F step 2b, K-5 and K-6): owed, awaiting approval, available, overdue and by age.
//
// RESPONSIBILITIES:
// - Add up the open items of the account
//
// IMPORTANT NOTES:
// - Read from its invoices; ages count days past due: current (not yet due), 1-30, 31-60, 61-90, over 90
// =========================================================
public sealed record OperationsCreditPosition(decimal Owed, decimal Awaiting, decimal Available, decimal Overdue, IReadOnlyList<decimal> Ages)
{
    public static OperationsCreditPosition Of(OperationsCreditAccount? account, IEnumerable<OperationsInvoice> invoices, DateOnly today)
    {
        ArgumentNullException.ThrowIfNull(invoices);
        List<OperationsInvoice> open = invoices.Where(invoice => invoice.Status == OperationsInvoiceStatus.Issued && invoice.Owed > 0m).ToList();
        List<OperationsInvoice> charged = open.Where(invoice => invoice.OnAccount == OperationsAccountCharge.Charged).ToList();
        decimal owed = charged.Sum(invoice => invoice.Owed);
        decimal awaiting = open.Where(invoice => invoice.OnAccount == OperationsAccountCharge.AwaitingApproval).Sum(invoice => invoice.Owed);
        decimal[] ages = new decimal[5];

        foreach (OperationsInvoice invoice in charged)
        {
            int late = invoice.DueOn is { } due ? today.DayNumber - due.DayNumber : 0;
            ages[late <= 0 ? 0 : late <= 30 ? 1 : late <= 60 ? 2 : late <= 90 ? 3 : 4] += invoice.Owed;
        }

        return new OperationsCreditPosition(owed, awaiting, Math.Max(0m, (account?.Limit ?? 0m) - owed - awaiting), ages.Skip(1).Sum(), ages);
    }
}
