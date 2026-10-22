using Vixly.Operations.Application.Messaging;
using Vixly.Operations.Application.Ports;
using Vixly.Operations.Domain.Billing;
using Vixly.Operations.Domain.Settings;

namespace Vixly.Operations.Application.Billing;

// =========================================================
// FILE: OperationsStatementNotices.cs
//
// PURPOSE:
// The monthly statement notices of one company (Slice 5F step 2b, K-7).
//
// RESPONSIBILITIES:
// - Once a month, each account with something owed gets its notice; every account is marked for the month
//
// IMPORTANT NOTES:
// - Run by the statement worker in the company's scope; the notices go through the outbox in the same save
// - The company can switch them off (Settings → Credit)
// =========================================================
public sealed class OperationsStatementNotices
{
    private const int Batch = 500;

    private const int MaxRounds = 100;

    private readonly IOperationsCreditRepository _credit;

    private readonly IOperationsSettingsRepository _settings;

    private readonly OperationsOutboxWriter _outbox;

    private readonly IOperationsUnitOfWork _unitOfWork;

    public OperationsStatementNotices(IOperationsCreditRepository credit, IOperationsSettingsRepository settings, OperationsOutboxWriter outbox, IOperationsUnitOfWork unitOfWork)
    {
        _credit = credit ?? throw new ArgumentNullException(nameof(credit));
        _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        _outbox = outbox ?? throw new ArgumentNullException(nameof(outbox));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<int> SendAsync(Guid tenantId, string portalSlug, DateTimeOffset utcNow, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(portalSlug);

        if (await _settings.FindAsync(cancellationToken) is { CreditStatements: false })
        {
            return 0;
        }

        DateOnly today = OperationsCreditBook.Today((await _settings.FindAsync(cancellationToken))!, utcNow);
        string month = today.ToString("yyyy-MM", System.Globalization.CultureInfo.InvariantCulture);
        int sent = 0;

        // In batches until every account is marked for the month (review: never only the newest ones).
        for (int round = 0; round < MaxRounds; round++)
        {
            IReadOnlyList<OperationsCreditAccount> due = await _credit.ListStatementsDueAsync(month, Batch, cancellationToken);

            if (due.Count == 0)
            {
                break;
            }

            ILookup<Guid, Domain.Billing.OperationsInvoice> invoices = (await _credit.ListAccountInvoicesAsync(due.Select(account => account.CustomerId).ToList(), cancellationToken))
                .ToLookup(invoice => invoice.CustomerId);
            int batch = 0;

            foreach (OperationsCreditAccount account in due)
            {
                account.StatementSent(month, utcNow);

                if (OperationsCreditPosition.Of(account, invoices[account.CustomerId], today).Owed > 0m)
                {
                    _outbox.Write(OperationsStatementNotice.MessageType, tenantId, account.Id, new OperationsStatementNotice(account.CustomerId, portalSlug));
                    batch++;
                }
            }

            if (!await _unitOfWork.SaveChangesAsync(cancellationToken))
            {
                break;
            }

            sent += batch;
        }

        return sent;
    }
}
