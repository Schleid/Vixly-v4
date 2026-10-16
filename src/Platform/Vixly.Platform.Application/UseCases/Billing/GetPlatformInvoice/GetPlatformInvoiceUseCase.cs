using Vixly.Platform.Application.Ports;
using Vixly.Platform.Application.Queries;
using Vixly.Platform.Application.Security;
using Vixly.Platform.Application.UseCases.Billing;
using Vixly.Platform.Domain.Billing;
using Vixly.Shared.Kernel.Domain;
using Vixly.Shared.Kernel.Errors;
using Vixly.Shared.Kernel.Localization;
using Vixly.Shared.Kernel.Paging;

namespace Vixly.Platform.Application.UseCases.Billing.GetPlatformInvoice;

// =========================================================
// FILE: GetPlatformInvoiceUseCase.cs
//
// PURPOSE:
// Reads one invoice with its lines, payments, credit notes and history.
//
// RESPONSIBILITIES:
// - Check the permission first (gate), then run the rule
// - Return a response; never throw for an expected refusal
//
// IMPORTANT NOTES:
// - One use case per behaviour (Global Standard 5.1)
// =========================================================
public sealed class GetPlatformInvoiceUseCase
{
    private readonly PlatformBillingAdministration _billing;

    private readonly IPlatformSubscriptionQueries _names;

    private readonly IPlatformTenantRepository _tenants;

    private readonly IPlatformInvoiceHistory _history;

    public GetPlatformInvoiceUseCase(
        PlatformBillingAdministration billing,
        IPlatformSubscriptionQueries names,
        IPlatformTenantRepository tenants,
        IPlatformInvoiceHistory history)
    {
        _billing = billing ?? throw new ArgumentNullException(nameof(billing));
        _names = names ?? throw new ArgumentNullException(nameof(names));
        _tenants = tenants ?? throw new ArgumentNullException(nameof(tenants));
        _history = history ?? throw new ArgumentNullException(nameof(history));
    }

    public async Task<GetPlatformInvoiceResponse> ExecuteAsync(
        PlatformAuthorizationContext context,
        GetPlatformInvoiceRequest request,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(request);

        if (await _billing.AuthorizeAsync(context, PlatformPermissions.BillingView, requireStepUp: false, cancellationToken) is { } refused)
        {
            return GetPlatformInvoiceResponse.Failure(refused);
        }

        PlatformInvoice? invoice = await _billing.Invoices.GetAsync(request.PlatformInvoiceId, cancellationToken);

        if (invoice is null)
        {
            return GetPlatformInvoiceResponse.Failure(PlatformBillingErrors.InvoiceNotFound);
        }

        PlatformInvoiceAbilities abilities = await PlatformBillingAbilities.ReadAsync(_billing, context, cancellationToken);
        IReadOnlyList<PlatformInvoiceEvent> events = await _history.ListAsync(invoice.Id, cancellationToken);
        IReadOnlyDictionary<Guid, string> names = await _names.StaffNamesAsync(
            invoice.Payments.SelectMany(item => new[] { item.RecordedByUserId, item.ReversedByUserId })
                .Concat(invoice.Credits.SelectMany(item => new Guid?[] { item.RequestedByUserId, item.DecidedByUserId }))
                .Concat(events.Select(item => item.ActorUserId))
                .OfType<Guid>()
                .Distinct(),
            cancellationToken);
        string? Name(Guid? id) => id is Guid value && names.TryGetValue(value, out string? name) ? name : null;
        var tenant = await _tenants.GetAsync(invoice.PlatformTenantId, cancellationToken);

        PlatformInvoiceDetail detail = new(
            invoice.Id,
            invoice.Number,
            invoice.PlatformTenantId,
            tenant?.Company.TradingName ?? tenant?.Company.LegalName ?? invoice.Customer.Name,
            invoice.PlatformSubscriptionId,
            invoice.Kind,
            invoice.Status,
            invoice.CurrencyCode,
            invoice.Language.Value,
            invoice.Customer.Name,
            invoice.Customer.Address,
            invoice.Customer.TaxId,
            invoice.Customer.Email,
            invoice.IssuedUtc,
            invoice.DueUtc,
            invoice.PeriodStartUtc,
            invoice.PeriodEndUtc,
            invoice.NetTotal,
            invoice.DiscountTotal,
            invoice.TaxTotal,
            invoice.Total,
            invoice.PaidTotal,
            invoice.CreditedTotal,
            invoice.Balance,
            invoice.RefundOwed,
            invoice.IsOverdueAt(_billing.UtcNow),
            invoice.Lines.OrderBy(line => line.Position)
                .Select(line => new PlatformInvoiceLineItem(line.Kind, line.TextKey, line.Arguments, line.Quantity, line.UnitPrice, line.Net, line.TaxRate, line.TaxLabel, line.Tax))
                .ToArray(),
            invoice.Payments.OrderBy(item => item.CreatedUtc)
                .Select(item => new PlatformPaymentItem(item.Id, item.ReceiptNumber, item.Amount, item.Method, item.ReceivedUtc, item.Reference, item.Note, Name(item.RecordedByUserId), item.Status, item.ReversalReason, Name(item.ReversedByUserId)))
                .ToArray(),
            invoice.Credits.OrderBy(item => item.CreatedUtc)
                .Select(item => new PlatformCreditNoteItem(
                    item.Id, item.Number, item.Amount, item.TaxPart, item.IsVoid, item.Reason, item.Status, Name(item.RequestedByUserId), item.CreatedUtc,
                    Name(item.DecidedByUserId), item.DecidedUtc, item.DecisionReason,
                    item.IsPending && abilities.CanApproveCredits && item.RequestedByUserId != context.PlatformUserId))
                .ToArray(),
            events.OrderByDescending(item => item.CreatedUtc)
                .Select(item => new PlatformInvoiceEventItem(item.EventCode, item.Arguments, Name(item.ActorUserId), item.Reason, item.CreatedUtc))
                .ToArray());

        return GetPlatformInvoiceResponse.Success(detail, abilities);
    }
}
