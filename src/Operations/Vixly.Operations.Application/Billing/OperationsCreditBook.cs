using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Locations;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsCreditBook.cs
//
// PURPOSE:
// Reads a customer's credit account with where it stands (Slice 5F step 2b, K-5).
//
// RESPONSIBILITIES:
// - Load the account, its open invoices and its position on the company's day
//
// IMPORTANT NOTES:
// - Read only; the company's day is in its time zone
// =========================================================
public sealed class OperationsCreditBook
{
    private readonly IOperationsCreditRepository _credit;

    public OperationsCreditBook(IOperationsCreditRepository credit)
    {
        _credit = credit ?? throw new ArgumentNullException(nameof(credit));
    }

    public static DateOnly Today(OperationsTenantSettings settings, DateTimeOffset utcNow) => OperationsTimeZones.Today(utcNow, settings?.TimeZone);

    // The open items (still owed), oldest first, and the position.
    public async Task<(IReadOnlyList<Domain.Billing.OperationsInvoice> Items, OperationsCreditPosition Position)> ReadAsync(OperationsCreditAccount? account, Guid customerId, DateOnly today,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<Domain.Billing.OperationsInvoice> invoices = await _credit.ListAccountInvoicesAsync(new[] { customerId }, cancellationToken);
        List<Domain.Billing.OperationsInvoice> open = invoices.Where(invoice => invoice.Owed > 0m).OrderBy(invoice => invoice.DueOn ?? invoice.IssuedOn).ThenBy(invoice => invoice.Number, StringComparer.Ordinal).ToList();
        return (open, OperationsCreditPosition.Of(account, invoices, today));
    }
}
