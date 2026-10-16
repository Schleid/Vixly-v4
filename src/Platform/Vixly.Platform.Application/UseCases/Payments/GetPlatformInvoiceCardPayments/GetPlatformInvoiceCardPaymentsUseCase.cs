using Vixly.Platform.Application.Payments;
using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Payments;
using Vixly.Platform.Domain.Billing;
using Vixly.Platform.Domain.Payments;
using Vixly.Platform.Domain.Tenants;
using Vixly.Shared.Kernel.Domain;

namespace Vixly.Platform.Application.UseCases.Payments.GetPlatformInvoiceCardPayments;

// =========================================================
// FILE: GetPlatformInvoiceCardPaymentsUseCase.cs
//
// PURPOSE:
// Reads the card side of an invoice: transactions, the card that would be charged and what can be refunded.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformInvoiceCardPaymentsUseCase
{
    private readonly PlatformPaymentsAdministration _payments;

    private readonly IPlatformPaymentsQueries _queries;

    public GetPlatformInvoiceCardPaymentsUseCase(
        PlatformPaymentsAdministration payments,
        IPlatformPaymentsQueries queries)
    {
        _payments = payments ?? throw new ArgumentNullException(nameof(payments));
        _queries = queries ?? throw new ArgumentNullException(nameof(queries));
    }

    public async Task<GetPlatformInvoiceCardPaymentsResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformInvoiceCardPaymentsRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _payments.AuthorizeAsync(context, PlatformPermissions.BillingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformInvoiceCardPaymentsResponse.Failure(refused);
        }

        PlatformInvoice? invoice = await _payments.Billing.Invoices.GetAsync(request.PlatformInvoiceId, cancellationToken);

        if (invoice is null)
        {
            return GetPlatformInvoiceCardPaymentsResponse.Failure(PlatformBillingErrors.InvoiceNotFound);
        }

        DateTimeOffset utcNow = _payments.UtcNow;
        IReadOnlyList<PlatformCardTransaction> transactions = await _payments.Store.ListForInvoiceAsync(invoice.Id, cancellationToken);
        var listed = await _queries.ListTransactionsAsync(
            new Vixly.Platform.Application.Queries.PlatformCardTransactionFilter(null, null, null, invoice.Id, false),
            Vixly.Shared.Kernel.Paging.PagedRequest.Create(1, Vixly.Shared.Kernel.Paging.PagedRequest.MaximumPageSize),
            cancellationToken);
        PlatformSavedCard? card = await _payments.DefaultCardAsync(invoice.PlatformTenantId, cancellationToken);
        bool ready = await _payments.RefusalForAsync(invoice.CurrencyCode, cancellationToken) is null;
        bool canCharge = ready && card is not null && !card.IsExpiredAt(utcNow) && invoice.Status == PlatformInvoiceStatus.Issued && invoice.Balance > 0m
            && transactions.All(item => !item.IsOpen || item.Kind != PlatformCardTransactionKind.Charge)
            && await _payments.HoldsAsync(context, PlatformPermissions.BillingChargeCards, cancellationToken);

        return GetPlatformInvoiceCardPaymentsResponse.Success(new PlatformInvoiceCardPaymentsItem(
            listed.Items,
            card is null ? null : PlatformPaymentsAdministration.CardItem(card, utcNow),
            ready,
            canCharge,
            await _payments.HoldsAsync(context, PlatformPermissions.BillingRefundCards, cancellationToken),
            PlatformCardRefunds.Refundable(invoice, transactions)));
    }
}
